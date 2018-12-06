if [ -f '/var/Exchange/POS1.rep' ]; then 
curl -X POST -F "Kassa545=@/var/Exchange/POS1.rep" 10.252.13.84:80; 
rm -f '/var/Exchange/POS1.rep';
fi