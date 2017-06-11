# RFC: CCS

## CAR COMMUNICATION SYSTEM
Protocol specification  
May 2017  
Daniele Di Sarli, Diego Giorgini

## Overview

### Features
This protocol provides two features:

 - *ACQUISITION:*
   to acquire information which is not detectable by the visual subsystem,
   such as the model and the turn intention of the cars in the proximity;
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

Let's suppose the following:

  1. the vehicle *A* has detected with the visual subsystem all the other
     vehicles in the crossroad
  2. *A* knows all the nodes on the network (i.e. it has received a
     KeepAlive message from them). *A* needs to associate the detected
     vehicles with the information received from the wireless network.

### State 1) Begin

*A* is in the `Begin` state. *A* waits some time: in regular conditions it waits a
small desynchronization time useful to avoid that different vehicles transmit at the same time;
if instead it has recently received a FCT telling it to wait the end of another
CCS procedure, it sets the waiting timer to the duration of a procedure plus a
random backoff. At this point *A* chooses a node of the network with a round robin
policy and sends a CCS message with its address and goes to the `Wait_to_blink` state.

While waiting, *A* may receive CCS and FCT messages:

 - If *A* receives a CCS message with its own address it starts the procedure with
   the sender and transitions to `Wait_to_blink`.
 - If *A* receives a FCT it sets the waiting timer to the length of a procedure plus
   a random backoff.

### State 2) Wait_to_blink

The vehicle remains in this state for a time of *X*. During this time, the vehicle
listens to non-pardoned FCT messages which will abort the CCS procedure.

If a non-pardoned FCT is received, it means that another CCS procedure was already
active between some other vehicles. A random backoff time *B* (between 1 and *Z* milliseconds)
must be set. The procedure is aborted and the vehicle can send a new CCS request only after
*2X + B* milliseconds.

If a CCS is received such that the request is not for me or the sender is different than
the peer I'm currently interacting with, then a broadcast FCT message must be sent. The
pardoned address of the FCT message must be the one of the peer we're interacting with.

If no FCT is received during this time, the vehicle goes to the `Blink` state.

### State 3) Blink






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
**Sender Addr:**   the address that identifies the vehicle that emitted the KeepAlive.  
**Requested Act:** the action that the vehicle would like to perform.  
**Current Act:**   the action that the vehicle is performing, after coordination over the network.  
**Manufacturer:**  the manufacturer of the vehicle.  
**Model:**         the model of the vehicle.  
**Priority:**      (boolean) whether the action requested by the vehicle has priority over those of the
                   other vehicles.  

### CCS

     0                   1                   2
     0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3
    +---------------+---------------+---------------+
    | Msg Type (C)  | Receiver Addr | Sender Addr   |
    +---------------+---------------+---------------+

**Msg Type:**      constant value 67 (ASCII code for character `C`).  
**Receiver Addr:** the address that identifies the vehichle to which the CCS is addressed.  
**Sender Addr:**   the address that identifies the vehicle that emitted the CCS.

### FCT

     0                   1
     0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5
    +---------------+---------------+
    | Msg Type (S)  | Pardoned Addr |
    +---------------+---------------+

**Msg Type:**      constant value 83 (ASCII code for character `S`).  
**Pardoned Addr:** the address that identifies the other vehicle that is currently engaged in 
                   the execution of a CCS procedure, which therefore must ignore this message.