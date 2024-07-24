namespace N2NGO_Server
{
    internal class Program
    {
        static void InitializeConsoleInterface(MineMP.ConsoleBuffer buffer)
        {
            buffer.ReadingInputLinePeeking += ConsoleBuffer_ReadingInputLinePeeking;
            buffer.ReadingInputLine += ConsoleBuffer_ReadingInputLine;
            buffer.ControlSymbolBufferPushed += ConsoleBuffer_ControlSymbolBufferPushed;
            buffer.DebugBufferAppended += ConsoleBuffer_DebugBufferAppended;
            buffer.ErrorBufferAppended += ConsoleBuffer_ErrorBufferAppended;
            buffer.InfoBufferAppended += ConsoleBuffer_InfoBufferAppended;
            buffer.WarnBufferAppended += ConsoleBuffer_WarnBufferAppended;
        }

        static void Main(string[] args)
        {
            string config = args.Length > 0 ? args[0] : "config.ini";

            N2NGOServer.ServerInConsole server;
            MineMP.ConsoleBuffer consoleBuffer;

            consoleBuffer = new();
            InitializeConsoleInterface(consoleBuffer);

            server = new N2NGOServer.ServerInConsole(consoleBuffer, config);
            server.Initialize();

            server.RunSync();
        }

        private static void ConsoleBuffer_ReadingInputLinePeeking(object sender)
        {
            return; //Ignore
        }

        private static void ConsoleBuffer_ReadingInputLine(object sender)
        {
            var input = Console.ReadLine();
            ((MineMP.ConsoleBuffer)sender).MakeInputLine(input == null ? "" : input);
        }

        private static void ConsoleBuffer_ControlSymbolBufferPushed(object sender, MineMP.ConsoleBuffer.ConsoleControlSymbolBufferPushedEventArgs e)
        {
            switch (e.BufferPushed)
            {
                default: return;

                case MineMP.ConsoleBuffer.ControlSymbols.ClearScreen:
                    Console.Clear();
                    break;
            }

            return;
        }

        private static void ConsoleBuffer_DebugBufferAppended(object sender, MineMP.ConsoleBuffer.ConsoleBufferAppendEventArgs e)
        {
            Console.BackgroundColor = ConsoleColor.Magenta;
            Console.Out.Write(e.BufferAppended);
            Console.ResetColor();
        }

        private static void ConsoleBuffer_WarnBufferAppended(object sender, MineMP.ConsoleBuffer.ConsoleBufferAppendEventArgs e)
        {
            Console.BackgroundColor = ConsoleColor.DarkYellow;
            Console.Out.Write(e.BufferAppended);
            Console.ResetColor();
        }

        private static void ConsoleBuffer_InfoBufferAppended(object sender, MineMP.ConsoleBuffer.ConsoleBufferAppendEventArgs e)
        {
            Console.BackgroundColor = ConsoleColor.DarkGray;
            Console.Out.Write(e.BufferAppended);
            Console.ResetColor();
        }

        private static void ConsoleBuffer_ErrorBufferAppended(object sender, MineMP.ConsoleBuffer.ConsoleBufferAppendEventArgs e)
        {
            Console.BackgroundColor = ConsoleColor.Red;
            Console.Error.Write(e.BufferAppended);
            Console.ResetColor();
        }
    }
}
