# Server4Curl

Клинеты отправляют файлы методом POST:

curl -X POST -F "ИМЯ_КЛИЕНТА=@/var/Exchange/File.txt" XXX.XXX.XXX.XXX:80; 

Задача: 

Принять и разложить файлы по папкам (имя=имя клиента).

