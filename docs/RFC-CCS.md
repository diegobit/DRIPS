# RFC: CCS

## CAR COMMUNICATION SYSTEM
Protocol specifications  
May 2017  
Daniele Di Sarli, Diego Giorgini

## Overview

### Features
This protocol provides two features:

 - *ACQUISITION:*
   to know the non-detectable information from the visual subsystem of
   the cars in the vicinity, such as their model and their turn intention;
 - *ASSOCIATION:*
   to associate a node from which a message from the network has been received
   with a car detected with the visual subsystem.

### Notation

To make this document easier to understand, not every detail will be explained
the protocol. For the same reason, some parameters have different names from the
actual implementation. The correspondence between names from this document
and the implementation can be found in the following list: 

 * X: TIMESPAN_X
 * Z: TIMESPAN_MAX_BACKOFF

 ### Messages

 Three types of messages are defined:
  
  * KeepAlive:
      sent periodically to allow nodes discoverability; it broadcasts all
      the information of the sender, including the turn intention of the car;
  * CCS (Car Communication Signal):
      sent to start the association procedure with a node of the network;
  * FCT (Force Communication Termination):
      sent to notify everyone that another procedure is ongoing in order to
      ensure only one procedure is active at any time.

## Protocol description

Let's suppose: (1) the car A has detected with the visual subsystem all the other
cars in the crossroad; (2) A knows all the nodes on the network (i.e. it has
received a KeepAlive message from them). A needs to associate detected cars
with the information received from the wireless network.

### State 1) Begin

A is in the state `Begin`. A waits some time: in regular conditions it waits a
small desynchronization time useful to avoid cars to transmit at the same time;
if instead it has recently received a FCT telling it to wait the end of another
CCS procedure, it sets the waiting timer to the duration of a procedure plus a
random backoff. At this point A choses a node of the network with a round robin
policy and sends a CCS message with its address and goes to `Wait_to_blink`.

While waiting, A may receive CCS and FCT messages:

 - If A receives a CCS message with its own address it starts the procedure with
   the sender and transitions to `Wait_to_blink`.
 - If A receives a FCT it sets the waiting timer to the length of a procedure plus
   a random backoff.

### State 2) Wait_to_blink





Per fare ciò, l'auto A deve iniziare la procedura di CCS.

 1. L'auto invia in broadcast un pacchetto CCS con specificato l'indirizzo dell'auto
    che A vuole associare.
 2. L'auto attende X secondi.
 3. L'auto entra nella fase di rilevamento della frequenza tramite sensori IR.
    L'auto si aspetta di osservare per una durata di altri X secondi una frequenza
    di K Hz su uno o più sensori. Contemporaneamente, inizia anch'essa a far
    lampeggiare i led con una frequenza di K Hz per una durata di X secondi.
 4. Dopo lo scadere degli X secondi, l'auto interpreta i dati rilevati e li associa
    al nodo a cui è stata fatta la richiesta. Dopodiché torna nello stato di
    operatività standard.

In ogni momento della procedura di CCS, l'auto A rimane in ascolto di messaggi FCT. In
caso di ricezione di tale messaggio, e in caso in cui il Pardoned Address del messaggio
sia diverso da quello dell'auto A, la procedura viene interrotta e ripresa dopo un
tempo di 2X sommato a un backoff casuale compreso tra 1 e Z millisecondi.


Ogni altra auto nell'incrocio, alla ricezione del CCS di A:

 1. Determina se attualmente è impegnata in una procedura di CCS con un qualsiasi peer.
     a. Se sì e se quel peer non è A, oppure se sì e se il CCS non era per questa auto,
        allora risponde in broadcast con un FCT con specificato l'indirizzo del peer con
        cui è in corso la procedura già avviata.
     b. Se no, determina se il CCS era destinato a lei.
          i. Se no, termina
         ii. Se sì, attende X secondi da quando ha rilevato il CCS, quindi entra
             nella fase di rilevamento della frequenza tramite sensori IR.
             L'auto si aspetta di osservare per una durata di altri X secondi una frequenza
             di K Hz su uno o più sensori. Contemporaneamente, inizia anch'essa a far
             lampeggiare i led con una frequenza di K Hz per una durata di X secondi.

Durante tutto il tempo in cui si trova nello stato 1.b.ii., l'auto rimane in ascolto di
messaggi FCT. In caso di ricezione di tale messaggio, e in caso in cui il Pardoned Address
del messaggio sia diverso da quello dell'auto corrente, la procedura viene interrotta.

## Formato dei pacchetti

### KeepAlive

```
  0                   1                   2                   3
  0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
 +---------------+---------------+-------------------------------+
 | Msg Type (K)  |  Sender Addr  | Requested Act |  Current Act  |
 +---------------+---------------+-------------------------------+
 |                                                               |
 +                         Manufacturer                          +
 |                                                               |
 +---------------------------------------------------------------+
 |                                                               |
 +                             Model                             +
 |                                                               |
 +---------------+-----------------------------------------------+
 |   Priority    |
 +---------------+
```

 Msg Type      = 75 (carattere K)  
 Sender Addr   = l'indirizzo che identifica l'auto che ha emesso il CCS.  
 Requested Act = l'azione che l'auto vorrebbe fare  
 Current Act   = l'azione che l'auto sta facendo a seguito della coordinazione con la rete  
 Manufacturer  = il produttore dell'auto  
 Model         = il modello dell'auto  
 Priority      = (boolean) se l'azione richiesta dall'auto ha priorità su quelle delle altre auto  

### CCS

```
  0                   1                   2
  0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3
 +---------------+---------------+---------------+
 | Msg Type (C)  | Receiver Addr | Sender Addr   |
 +---------------+---------------+---------------+
 ```

 Msg Type = 67 (carattere C)  
 Receiver Addr = l'indirizzo che identifica l'auto è cui è rivolto il CCS.  
 Sender Addr = l'indirizzo che identifica l'auto che ha emesso il CCS.

### FCT

```
  0                   1
  0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5
 +---------------+---------------+
 | Msg Type (S)  | Pardoned Addr |
 +---------------+---------------+
```

 Msg Type      = 83 (carattere S)  
 Pardoned Addr = l'indirizzo che identifica l'auto che è attualmente impegnata nell'esecuzione
                 di una procedura CCS, e che quindi deve ignorare questo messaggio.
