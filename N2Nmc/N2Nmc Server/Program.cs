using MineMP;
using static MineMP.ConsoleBuffer;

namespace N2Nmc_Server
{
    internal class Program
    {
        static void Main(string[] args)
        {
            MineMP.ConsoleBuffer consoleBuffer = new MineMP.ConsoleBuffer();
            consoleBuffer.ReadingInputLinePeeking += ConsoleBuffer_ReadingInputLinePeeking;
            consoleBuffer.ReadingInputLine += ConsoleBuffer_ReadingInputLine;
            consoleBuffer.ControlSymbolBufferPushed += ConsoleBuffer_ControlSymbolBufferPushed;
            consoleBuffer.DebugBufferAppended += ConsoleBuffer_DebugBufferAppended;
            consoleBuffer.ErrorBufferAppended += ConsoleBuffer_ErrorBufferAppended;
            consoleBuffer.InfoBufferAppended += ConsoleBuffer_InfoBufferAppended;
            consoleBuffer.WarnBufferAppended += ConsoleBuffer_WarnBufferAppended;

            N2NmcServer.ServerInConsole server = new N2NmcServer.ServerInConsole(consoleBuffer);
            server.Init();
            server.RunSync();
        }

        private static void ConsoleBuffer_ReadingInputLinePeeking(object sender)
        {
            return; //Ignore
        }

        private static void ConsoleBuffer_ReadingInputLine(object sender)
        {
            ((ConsoleBuffer)sender).MakeInputLine(Console.ReadLine());
        }

        private static void ConsoleBuffer_ControlSymbolBufferPushed(object sender, ConsoleControlSymbolBufferPushedEventArgs e)
        {
            switch (e.BufferPushed)
            {
                default: return;

                case ControlSymbols.ClearScreen:
                    Console.Clear();
                    break;
            }

            return;
        }

        private static void ConsoleBuffer_DebugBufferAppended(object sender, ConsoleBuffer.ConsoleBufferAppendEventArgs e)
        {
            Console.BackgroundColor = ConsoleColor.Magenta;
            Console.Out.Write(e.BufferAppended);
            Console.ResetColor();
        }

        private static void ConsoleBuffer_WarnBufferAppended(object sender, ConsoleBuffer.ConsoleBufferAppendEventArgs e)
        {
            Console.BackgroundColor = ConsoleColor.DarkYellow;
            Console.Out.Write(e.BufferAppended);
            Console.ResetColor();
        }

        private static void ConsoleBuffer_InfoBufferAppended(object sender, ConsoleBuffer.ConsoleBufferAppendEventArgs e)
        {
            Console.BackgroundColor = ConsoleColor.DarkGray;
            Console.Out.Write(e.BufferAppended);
            Console.ResetColor();
        }

        private static void ConsoleBuffer_ErrorBufferAppended(object sender, ConsoleBuffer.ConsoleBufferAppendEventArgs e)
        {
            Console.BackgroundColor = ConsoleColor.Red;
            Console.Error.Write(e.BufferAppended);
            Console.ResetColor();
        }
    }
}