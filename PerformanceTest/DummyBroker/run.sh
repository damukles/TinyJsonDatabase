#!/bin/bash
for i in 1 2 3 4 5 6 7 8 9 10
do
   exec node app.js $i &
done

read -n1 -r -p "Press any key to stop..."

ps aux | grep node | cut -c-10 | while read i; do kill $i; done