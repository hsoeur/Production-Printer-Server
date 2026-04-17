namespace Api.PrinterProd.Services.Printer
{
    public interface IPrinter
    {
        public void ExecutePrinting(string usage);
        public void PrintFtp();

        public bool PrintFileTest(string usage);
        public bool PrintBySerialNumberFtp();
    }
}
