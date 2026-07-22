namespace Api.PrinterProd.Services.Printer
{
    public interface IPrinter
    {
        public void ExecutePrinting(string usage, string filePath);
        public void PrintFtp(string filePath);

        public bool PrintFileTest(string usage, string filePath);
        public bool PrintBySerialNumberFtp(string filePath);
        public bool PrintBySerialNumberFtp(string machineName, string filePath);
        public bool IsSerialNumberPrinterConfigured(string machineName);
    }
}
