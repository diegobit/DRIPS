# RFC CCS

Supponiamo che l'auto A abbia rilevato tramite sensori IR tutte le auto nell'incrocio.
L'auto A ha anche ricevuto dei pacchetti su rete wireless in cui sono specificate delle
caratteristiche di alcune auto.

I messaggi inviati sono:

 * CCS - Chi Cazzo Sei
 * SCS - Stop Chi Sei

L'auto A vuole mettere in relazione ogni auto rilevata con i dati acquisiti sulla rete
wireless.

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

In ogni momento della procedura di CCS, l'auto A rimane in ascolto di messaggi SCS. In
caso di ricezione di tale messaggio, e in caso in cui il Pardoned Address del messaggio
sia diverso da quello dell'auto A, la procedura viene interrotta e ripresa dopo un
tempo di 2X sommato a un backoff casuale compreso tra Y e Z secondi.


Ogni altra auto nell'incrocio, alla ricezione del CCS di A:

 1. Determina se attualmente è impegnata in una procedura di CCS con un qualsiasi peer.
     a. Se sì, risponde in broadcast con un SCS con specificato l'indirizzo del peer con
        cui è in corso la procedura già avviata.
     b. Se no, determina se il CCS era destinato a lei.
          i. Se no, termina
         ii. Se sì, attende X secondi da quando ha rilevato il CCS, quindi entra
             nella fase di rilevamento della frequenza tramite sensori IR.
             L'auto si aspetta di osservare per una durata di altri X secondi una frequenza
             di K Hz su uno o più sensori. Contemporaneamente, inizia anch'essa a far
             lampeggiare i led con una frequenza di K Hz per una durata di X secondi.

Durante tutto il tempo in cui si trova nello stato 1.b.ii., l'auto rimane in ascolto di
messaggi SCS. In caso di ricezione di tale messaggio, e in caso in cui il Pardoned Address
del messaggio sia diverso da quello dell'auto corrente, la procedura viene interrotta.

## Formato dei pacchetti

### CCS

  0                   1                   2
  0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3
 +---------------+---------------+---------------+
 | Msg Type (C)  | Receiver Addr | Sender Addr   |
 +---------------+---------------+---------------+
 
 Msg Type = 67 (carattere C)
 Receiver Addr = l'indirizzo che identifica l'auto è cui è rivolto il CCS.
 Sender Addr = l'indirizzo che identifica l'auto che ha emesso il CCS.

### SCS

  0                   1
  0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5
 +---------------+---------------+
 | Msg Type (S)  | Pardoned Addr |
 +---------------+---------------+

 Msg Type = 83 (carattere S)
 Pardoned Addr = l'indirizzo che identifica l'auto che è attualmente impegnata nell'esecuzione
                 di una procedura CCS, e che quindi deve ignorare questo messaggio.
