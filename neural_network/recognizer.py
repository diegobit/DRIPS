import tensorflow as tf
import numpy as np
import pickle
import random

n_bins = 64
input_size = n_bins * 3
output_size = 8 # num of possible road configurations

# input: [...FFT0][...FFT1][...FFT2]
# output:
#
#   000 -> 1,0,0,0,0,0,0,0  (no cars)
#   001 -> 0,1,0,0,0,0,0,0  (just one car on the right)
#   010 -> 0,0,1,0,0,0,0,0  (just one car in front of us)
#   011 -> 0,0,0,1,0,0,0,0  (one car in front of us, and one on the right)
#   100 -> 0,0,0,0,1,0,0,0  (just one car on the left)
#   101 -> 0,0,0,0,0,1,0,0  ...
#   110 -> 0,0,0,0,0,0,1,0  ...
#   111 -> 0,0,0,0,0,0,0,1  ...

x = tf.placeholder(tf.float32, [None, input_size])

W = tf.Variable(tf.zeros([input_size, output_size]))
#W = tf.Variable(tf.truncated_normal([input_size, output_size], stddev=1./22.))
b = tf.Variable(tf.zeros([output_size]))

y = tf.nn.softmax(tf.matmul(x, W) + b)


# ===== Training =====

with open('data.pickle', 'rb') as f:
  data = pickle.load(f)
  random.shuffle(data)
  train_data = [v[0] for v in data]
  train_labels = [v[1] for v in data]

y_ = tf.placeholder(tf.float32, [None, output_size])
cross_entropy = tf.reduce_mean(tf.nn.softmax_cross_entropy_with_logits(labels=y_, logits=y))

train_step = tf.train.GradientDescentOptimizer(0.5).minimize(cross_entropy)


sess = tf.InteractiveSession()

tf.global_variables_initializer().run()

for i in range(1000):
  startpos = random.randint(0, 20000)
  batch_xs, batch_ys = train_data[startpos:startpos+100], train_labels[startpos:startpos+100]
  sess.run(train_step, feed_dict={x: batch_xs, y_: batch_ys})

correct_prediction = tf.equal(tf.argmax(y,1), tf.argmax(y_,1))
accuracy = tf.reduce_mean(tf.cast(correct_prediction, tf.float32))
print("Accuracy:")
print(sess.run(accuracy, feed_dict={x: train_data[:1000], y_: train_labels[:1000]}))

# Save weights
W_val, b_val = sess.run([W, b])
np.savetxt("W.csv", W_val, delimiter=",")
np.savetxt("b.csv", b_val, delimiter=",")