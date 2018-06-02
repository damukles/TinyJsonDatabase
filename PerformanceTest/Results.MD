# Performance Test

## Ohne Datenbank

* 10 Broker
* je alle 1ms eine Nachricht

=> d.h. 10'000 Messages pro Sekunde führt zu ca. 25% CPU-Last


## Mit LocalDb

* 10 Broker
* je alle 40ms eine Nachricht

=> d.h. 250 Messages pro Sekunde Single INSERT in Db ist da Maximum

Danach steigt die CPU-Last konstant an und be Beendigung wird noch einiges nachgeschrieben (EF kümmert sich um den Rückstand).


## Mit BlobDatabase

* 10 Broker
* je alle 5ms eine Nachricht

=> d.h. 2000 Messages pro Sekunde Insert() in Db geht gut (CPU ca. 50-70%)

Danach wird die CPU-Last sehr hoch, müsste noch gemessen werden, ob auch alle ankommenden noch verarbeitet werden können (Beendigung geschieht sofort, allerdings ist unklar, ob gelockte Calls noch bearbeitet werden beim Beenden).

