namespace api_lo_etiq.Services.Printer
{
    public interface IPrinter
    {
        public void ExecutePrinting(string usage);
        public void PrintFtp();

        public bool PrintFileTest(string usage);
    }
}
