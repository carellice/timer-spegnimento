<div align="center">

<img src="assets/logo.png" alt="Logo di Timer spegnimento" width="80" height="80">

# Timer spegnimento

**Scegli quando spegnere il PC. Al resto pensa Windows.**

Una piccola app per Windows, in italiano, per programmare lo spegnimento del computer e la chiusura delle finestre aperte.

[Scarica l’app](../../releases/latest) · [Come si usa](#come-si-usa) · [Compilazione](#compilazione)

![Piattaforma: Windows](https://img.shields.io/badge/piattaforma-Windows-0078D4)
![Linguaggio: C%23](https://img.shields.io/badge/linguaggio-C%23-512BD4)
![Interfaccia: Windows Forms](https://img.shields.io/badge/interfaccia-Windows%20Forms-008272)

</div>

## Cosa puoi fare

- **Programmare lo spegnimento** tra un certo numero di minuti oppure a un’ora precisa.
- **Chiedere la chiusura dei programmi prima dello spegnimento**, con un anticipo regolabile da 5 a 600 secondi.
- **Programmare solo la chiusura dei programmi**, senza spegnere il PC.
- **Chiudere subito le finestre aperte**, dopo una conferma.
- **Annullare i timer** e controllare il tempo rimanente.
- **Lasciare l’app nell’area di notifica** quando è attivo un timer.

## Download e avvio

1. Apri la pagina delle [release](../../releases/latest).
2. Scarica **`TimerSpegnimento.exe`** dagli allegati della release.
3. Avvia il file: non è prevista un’installazione.

L’app usa Windows Forms e il runtime **.NET Framework 4.x**. Lo script di compilazione usa il compilatore incluso nel .NET Framework di Windows; non richiede pacchetti NuGet.

## Come si usa

### Spegnere il computer

Nel riquadro **Spegnimento computer**, scegli **Tra** e indica i minuti, oppure seleziona **Alle ore** e imposta l’orario. Premi **Programma** per attivare lo spegnimento e **Annulla** per revocarlo.

Se vuoi chiedere ai programmi di chiudersi prima dello spegnimento, lascia selezionato **Chiudi i programmi** e scegli quanti secondi prima inviare la richiesta.

### Chiudere i programmi

Nel riquadro **Chiusura programmi**, imposta un intervallo o un orario e premi **Programma chiusura**. Questo timer è indipendente dallo spegnimento. **Chiudi tutto adesso** invia subito la richiesta di chiusura, dopo la conferma.

### Lasciare il timer in funzione

Con un timer attivo, la **X** nasconde la finestra nell’area di notifica. Fai doppio clic sull’icona per riaprirla oppure usa il menu con il tasto destro per scegliere **Apri** o **Esci**.

## Da sapere

> **Salva il lavoro prima di programmare lo spegnimento.** La richiesta iniziale di chiusura consente alle app di mostrare le proprie finestre di salvataggio, ma non garantisce che il lavoro venga salvato: alla scadenza, lo spegnimento di Windows può forzare la chiusura delle applicazioni.

- Lo spegnimento è affidato al comando di sistema `shutdown`. **Rimane programmato anche dopo l’uscita dall’app**. Per revocarlo, usa **Annulla** oppure esegui `shutdown /a` nel terminale.
- La chiusura dei programmi, sia autonoma sia anticipata rispetto allo spegnimento, funziona **solo mentre l’app è in esecuzione**, anche nell’area di notifica.
- Avviare un nuovo timer di spegnimento annulla prima un eventuale spegnimento già programmato nel sistema. Anche **Annulla** agisce sullo spegnimento di sistema.
- Se l’orario scelto è già passato, il timer viene impostato per il giorno successivo. L’intervallo in minuti va da 1 a 10.080, cioè 7 giorni.
- I timer di chiusura non vengono salvati tra un avvio e l’altro. Sospensione e ibernazione possono ritardare le operazioni: l’app non configura il risveglio del PC.
- La chiusura interessa le finestre principali visibili, comprese le cartelle di Esplora file. Desktop, barra delle applicazioni e alcuni componenti di sistema vengono esclusi; non è una terminazione di tutti i processi in background.

## Compilazione

Su Windows, dalla cartella del progetto:

```bat
build.bat
```

Lo script estrae l’icona di spegnimento di Windows e compila `TimerSpegnimento.cs` in `TimerSpegnimento.exe`, usando:

```text
%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
```

Serve quindi una versione a 64 bit di Windows con il compilatore .NET Framework presente in quel percorso. Il sorgente usa sintassi compatibile con C# 5.

## Il progetto

| File | Contenuto |
| --- | --- |
| `TimerSpegnimento.cs` | Interfaccia Windows Forms, timer e chiamate alle API Windows |
| `build.bat` | Compilazione dell’eseguibile e inclusione dell’icona |
| `assets/logo.png` | Icona estratta dall’app, usata in questo README |

L’icona visualizzata dall’app e nel README proviene da `shell32.dll` di Windows; non è un logo originale del progetto.
