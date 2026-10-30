## Timer spegnimento v1.1.0

- Nuovo menu **Azione** per scegliere se spegnere o riavviare il computer.
- Riavvio programmabile tra un numero di minuti o a un’ora precisa, con annullamento e chiusura anticipata dei programmi.
- Il conto alla rovescia e l’area di notifica mostrano l’azione effettivamente programmata.
- Cambiare la selezione non modifica un timer attivo: premi **Programma** per sostituirlo.

Scarica `TimerSpegnimento.exe` e avvialo su Windows con .NET Framework 4.x. `SHA256SUMS.txt` contiene l’impronta SHA-256 dell’eseguibile.

**Salva il lavoro prima di attivare il timer:** Windows può forzare la chiusura delle applicazioni. Uscire dall’app non annulla lo spegnimento o il riavvio già programmato; usa **Annulla** o `shutdown /a`. La chiusura anticipata dei programmi richiede che l’app rimanga aperta, anche nell’area di notifica.
