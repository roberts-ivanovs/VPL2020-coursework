CC=dotnet
OUTPUTDIR=./dist

simulaotr: frontendserver
	$(CC) publish frontendserver -c Release --output $(OUTPUTDIR)
