# RFC: CCS

## CAR COMMUNICATION SYSTEM
Specifiche del protocollo  
Maggio 2017  
Daniele Di Sarli, Diego Giorgini

## Funzionalità
Questo protocollo fornisce due funzionalità:

 - *ACQUISIZIONE:*  
   conoscere le caratteristiche non rilevabili con il sottosistema visivo delle auto nelle
   vicinanze, come il modello dell'auto e le sue intenzioni;
 - *ASSOCIAZIONE:*  
   associare un auto da cui si è ricevuto un messaggio sulla rete wireless con un'auto
   rilevata con il sottosistema visivo.

## Notazione
Di seguito utilizziamo nomi di parametri che, per brevità, non coincidono con i nomi utilizzati
nell'implementazione di riferimento del protocollo. Si elencano qui le associazioni tra i nomi
utilizzati in questo documento e il loro corrispettivo dell'implementazione.

 * X: TIMESPAN_X
 * Z: TIMESPAN_MAX_BACKOFF

## Descrizione del protocollo

I messaggi inviati sono di tre tipi:

 * KeepAlive:
     inviati periodicamente per lo scoperta e il mantenimento dei nodi nella rete
 * CCS (Chi Cazzo Sei, o Car Communication Signal):
     per avviare la procedura
 * FCT (Fatti i Cazzi Tuoi, o Force Communication Termination):
     per notificare altre auto che un'altra procedura è in corso nella rete

Supponiamo che l'auto A abbia rilevato tramite il sottosistema visivo tutte le auto nell'incrocio.
Il passo successivo è mettere in relazione ogni auto rilevata con i dati acquisiti sulla rete wireless.

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

 Msg Type      = 75 (carattere K)
 Sender Addr   = l'indirizzo che identifica l'auto che ha emesso il CCS.
 Requested Act = l'azione che l'auto vorrebbe fare
 Current Act   = l'azione che l'auto sta facendo a seguito della coordinazione con la rete
 Manufacturer  = il produttore dell'auto
 Model         = il modello dell'auto
 Priority      = (boolean) se l'azione richiesta dall'auto ha priorità su quelle delle altre auto

### CCS

  0                   1                   2
  0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3
 +---------------+---------------+---------------+
 | Msg Type (C)  | Receiver Addr | Sender Addr   |
 +---------------+---------------+---------------+
 
 Msg Type = 67 (carattere C)
 Receiver Addr = l'indirizzo che identifica l'auto è cui è rivolto il CCS.
 Sender Addr = l'indirizzo che identifica l'auto che ha emesso il CCS.

### FCT

  0                   1
  0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5
 +---------------+---------------+
 | Msg Type (S)  | Pardoned Addr |
 +---------------+---------------+

 Msg Type      = 83 (carattere S)
 Pardoned Addr = l'indirizzo che identifica l'auto che è attualmente impegnata nell'esecuzione
                 di una procedura CCS, e che quindi deve ignorare questo messaggio.
