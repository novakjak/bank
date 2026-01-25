# P2P Bank Node – README

## Popis projektu

P2P Bank Node je konzolová aplikace napsaná v .NET, která simuluje bankovní uzel v decentralizované P2P síti.
Uzel přijímá textové příkazy přes TCP, zpracovává je lokálně nebo je proxyuje na jiný bankovní uzel.
Součástí projektu je také monitoring a samostatná webová aplikace pro dohled nad stavem uzlu.

---

## Požadavky

- .NET SDK 9.0
- MySQL Server (volitelné – při výpadku se použije CSV)
- Git

---

## Struktura projektu

- src

  - hlavní bankovní uzel
  - síťová komunikace
  - persistence
  - monitoring
- web

  - monitorovací webová aplikace
- tests

  - unit testy
  - integrační testy
- docs

  - návrhová dokumentace
  - UML diagramy

---

## Konfigurace

Konfigurace se nachází v souboru `settings.ini`.

Obsahuje:

- timeouty sítě
- výchozí port
- MySQL connection string
- cestu k CSV souborům

Soubor musí být dostupný v runtime adresáři aplikace.

---

## Spuštění bankovního uzlu

Z kořenového adresáře projektu:

```
dotnet run --project src
```

Po spuštění:

- uzel otevře TCP port
- inicializuje persistence (MySQL / CSV)
- spustí monitoring service
- začne přijímat příkazy

---

## Spuštění monitorovacího webu

Webová aplikace slouží pouze ke čtení monitorovacích dat a řízení uzlu.

```
dotnet run --project web
```

Web zobrazuje:

- aktuální stav uzlu
- health state
- počet spojení
- command metriky

Web umožňuje:

- reset metrik
- požádat uzel o shutdown (přes databázi)

---

## Testy

### Unit testy

Testují jednotlivé komponenty izolovaně.

```
dotnet test tests/BankNode.UnitTests
```

---

### Integrační testy

Testují spolupráci více komponent dohromady.

```
dotnet test tests/BankNode.IntegrationTests
```

---

## Ukončení aplikace

Aplikaci lze ukončit:

- ručně (Ctrl+C)
- přes webové rozhraní
- nastavením `shutdown_requested` v databázi

---

## Poznámky

- MySQL je preferované úložiště
- CSV slouží jako fallback
- Monitoring web je read-only
