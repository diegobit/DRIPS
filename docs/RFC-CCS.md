# RFC: CCS

## CAR COMMUNICATION SYSTEM
Protocol specification  
June 2017  
Daniele Di Sarli, Diego Giorgini

## Overview

### Features
This protocol provides two features:

 - _ACQUISITION:_
   to acquire information which is not detectable by the visual subsystem, such
   as the model and the turn intention of the vehicles in the proximity;
 - _ASSOCIATION:_
   to associate a node from which a message from the network has been received
   with a vehicle detected by the visual subsystem.

### Notation

To make this document easier to understand, some parameters have different names
from the actual implementation. The correspondence between names from this
document and the implementation can be found in the following list:

 - _X_: TIMESPAN_X **TODO: Define constraints in another paragraph**
 - _Z_: TIMESPAN_MAX_BACKOFF **TODO: Define constraints in another paragraph**

### Messages

Three types of messages are defined:
  
 - **KeepAlive:**  
     Sent periodically to allow nodes discoverability; it broadcasts all the
     information of the sender, including the turn intention of the vehicle.
 - **CCS (Car Communication Signal):**  
     Sent to start the association procedure with a node of the network.
 - **FCT (Force Communication Termination):**  
     Sent to notify everyone that another procedure is ongoing in order to
     ensure only one procedure is active at any time.

## Protocol description

During the entire procedure we can receive KeepAlive messages. We use these to
update the cache of the vehicles in the network. Each vehicle in the cache must
expire after a specific interval of time which must obviously be greater than
the interval at which KeepAlive messages are sent.

The cache expiration time and the interval at which KeepAlive messages are sent
are left as implementation choices.

To detect the physical position of a node of the network, the protocol goes
through the following steps:

 1. The vehicle picks another vehicle _P_ from the list of detected nodes in the
    network and sends a broadcast CCS message with the address of _P_ in it. In
    alternative, the vehicle reacts to an incoming CCS request.
 2. The protocol is now symmetric. Both vehicles wait _X_ milliseconds to ensure
    that nobody else is already engaged in a CCS.
 3. Both vehicles advertise their position using the IR sensors for _X_
    milliseconds, and read the signal on the IR receivers to get the information
    advertised by the peer.
 4. Both vehicles interpretate the data sampled from the IR receivers so that
    they can deduce the physical position of each other. Then the protocol
    restarts from the beginning.

### State: Begin

In this state, the vehicle may send a CCS request to some node in the network,
or react to a CCS request.

The state begins by waiting some time: in regular conditions the vehicle waits a
small desynchronization time useful to avoid that different vehicles transmit at
the same time, thus helping to avoid collisions.

If instead there is a backoff _B_ set, then the vehicle waits _2X + B_
milliseconds. After the waiting time the vehicle chooses a node of the network
and sends a CCS message to it. It then goes to the `Wait_to_blink` state.

While in this state, the vehicle may receive CCS and FCT messages:

 - If a FCT is received, it means that another CCS procedure was already active
   between some other vehicles. A random backoff time _B_ (between 1 and _Z_
   milliseconds) must be set. The procedure is restarted and the vehicle can
   send a new CCS request only after _2X + B_ milliseconds.
 - If a CCS is received such that the request is for us, the vehicle goes to
   the `Wait_to_blink` state. If the request was not for us, then a random
   backoff time _B_ (between 1 and _Z_ milliseconds) must be set. The procedure
   is restarted and the vehicle can send a new CCS request only after _2X + B_
   milliseconds.

### State: Wait_to_blink

The vehicle remains in this state for a time of _X_ milliseconds. During this
time, the vehicle waits to make sure that nobody else is currently engaged in a
CCS, and also to ensure that nobody else starts a new CCS procedure in the
meantime.

In the following, by "non-pardoned FCT message" we mean a FCT message that does
not include the address of the receiver in the "Pardoned Addr" field (i.e. the
same message can be pardoned for some and non-pardoned for others).

While in this state, the vehicle may receive CCS and FCT messages:

 - If a non-pardoned FCT is received, it means that another CCS procedure was
   already active between some other vehicles. A random backoff time _B_
   (between 1 and _Z_ milliseconds) must be set. The procedure is aborted (jump
   to the `Begin` state) and the vehicle can send a new CCS request only after
   _2X + B_ milliseconds.
 - If a CCS is received such that the request is not for us or the sender is
   different than the peer we're currently interacting with, then a broadcast
   FCT message must be sent. The pardoned address of the FCT message must be the
   one of the peer we're interacting with.

If the procedure has not been aborted, after _X_ milliseconds have passed the
vehicle goes to the `Blink` state.

### State: Blink

The vehicle remains in this state for a time of _X_ milliseconds. During this
time, the vehicle will advertise its own position and look for the position of
the current peer.

To advertise its own position, the vehicle must blink its IR emitters at a
frequency of 1 KHz during the whole and only duration of this state.

To detect the position of the peer, after _X/2_ milliseconds have passed the
vehicle must sample the signal on the IR receivers. The values might need to be
saved in order to make them available to the `Interpretate` state.

While in this state, the vehicle may receive CCS and FCT messages:

 - If a non-pardoned FCT is received, it means that another CCS procedure was
   already active between some other vehicles. A random backoff time _B_
   (between 1 and _Z_ milliseconds) must be set. The procedure is aborted (jump
   to the `Begin` state) and the vehicle can send a new CCS request only after
   _2X + B_ milliseconds.
 - If a CCS is received such that the request is not for us or the sender is
   different than the peer we're currently interacting with, then a broadcast
   FCT message must be sent. The pardoned address of the FCT message must be the
   one of the peer we're interacting with.

If the procedure has not been aborted, after _X_ milliseconds have passed the
vehicle goes to the `Interpretate` state.

### State: Interpretate

The vehicle can now use the data acquired in the `Blink` state to determine the
position of the peer and combine the information obtained through the visual
subsystem.

While in this state, the vehicle may receive messages from the network:

 - If a CCS is received such that the request is for us, then a broadcast FCT
   message must be sent. The pardoned address of the FCT message must be "none"
   (e.g. a special address `0`).

After the state has finished its computations, the vehicle goes to the `Begin`
state.


--------------------------------------------------------------------------------


## Packet Format

### KeepAlive

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

**Msg Type:**      constant value 75 (ASCII code for character `K`).  
**Sender Addr:**   the address that identifies the vehicle that emitted the
                   KeepAlive.  
**Requested Act:** the action that the vehicle would like to perform.  
**Current Act:**   the action that the vehicle is performing, after coordination
                   over the network.  
**Manufacturer:**  the manufacturer of the vehicle.  
**Model:**         the model of the vehicle.  
**Priority:**      (boolean) whether the action requested by the vehicle has
                   priority over those of the other vehicles.  

### CCS

     0                   1                   2
     0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3
    +---------------+---------------+---------------+
    | Msg Type (C)  | Receiver Addr | Sender Addr   |
    +---------------+---------------+---------------+

**Msg Type:**      constant value 67 (ASCII code for character `C`).  
**Receiver Addr:** the address that identifies the vehicle to which the CCS is
                   addressed.  
**Sender Addr:**   the address that identifies the vehicle that emitted the CCS.

### FCT

     0                   1
     0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5
    +---------------+---------------+
    | Msg Type (S)  | Pardoned Addr |
    +---------------+---------------+

**Msg Type:**      constant value 83 (ASCII code for character `S`).  
**Pardoned Addr:** the address that identifies the other vehicle that is
                   currently engaged in the execution of a CCS procedure, which
                   therefore must ignore this message.