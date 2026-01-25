# P2P Bankovní uzel – návrh a implementace řešení

**Autor:** Martin Šilar
**Škola:** SPŠE Ječná
**Datum:** 25. 01. 2026
**Typ práce:** Školní projekt

## Účel systému

Cílem systému je simulovat bankovní uzel v decentralizované P2P síti bez centrální autority.
Každý uzel funguje autonomně, ale je schopen proxy komunikace s ostatními uzly.

---

## Kontext systému

Systém komunikuje s:

- TCP klienty (textové příkazy)
- jinými bankovními uzly v P2P síti
- databází MySQL
- souborovým systémem (CSV)
- monitorovací webovou aplikací

---

## Architektura

Aplikace je navržena jako vícevrstvá.

### Síťová vrstva

- NetworkListenerPřijímá TCP spojení a vytváří BankConnection.
- BankConnectionReprezentuje jedno TCP spojení.Zodpovídá za:

  - čtení vstupu
  - parsování zpráv
  - volání aplikační logiky
  - odesílání odpovědí
  - proxy komunikaci
- ConnectionManager
  Spravuje P2P spojení mezi bankovními uzly.

---

### Zprávy a příkazy

- MsgTypeIdentifikátor typu zprávy (AC, AD, AW, AB, AR, BN, BA, RP).
- IBankMsg
  Rozhraní pro všechny zprávy.
  Každá zpráva obsahuje vlastní logiku zpracování.

---

### Persistence

- IAccountStorageAbstraktní rozhraní pro práci s účty.
- MySqlAccountStoragePrimární úložiště dat.
- CsvAccountStorageZáložní úložiště při výpadku databáze.
- InMemoryAccountStoragePoužívá se pro testování.
- HybridAccountStorage
  Dynamicky přepíná mezi MySQL a CSV.

---

### Monitoring

- MetricsCollectorSbírá runtime metriky:

  - počet příkazů
  - chyby
  - průměrné časy zpracování
- MonitoringServicePeriodicky:

  - ukládá snapshoty
  - aktualizuje command metriky
  - kontroluje shutdown flag

---

## Sekvenční scénáře

### Lokální příkaz

1. Klient odešle TCP příkaz
2. BankConnection vytvoří IBankMsg
3. Zpráva je zpracována lokálně
4. Persistence vrátí výsledek
5. Odpověď je odeslána klientovi

---

### Proxy příkaz

1. Klient odešle příkaz s cizím bank code
2. BankConnection detekuje vzdálený uzel
3. ConnectionManager odešle proxy request
4. Vzdálený uzel příkaz zpracuje
5. Response se vrátí zpět
6. Klient obdrží odpověď

---

## Deployment

- Bankovní uzel: .NET konzolová aplikace
- Databáze: MySQL server
- CSV: lokální soubory
- Monitoring web: samostatná ASP.NET Core aplikace

---

## Testování

- Unit testy: izolované komponenty
- Integrační testy: spolupráce modulů
- Síťová vrstva je mockovatelná
