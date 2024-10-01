using System.Diagnostics;

namespace N2NGOServer;

internal class Program
{
    static Process StartSupernode()
    {
        Process process = new();
        process.StartInfo.FileName = "Storage/Utilities/supernode.exe";
        process.Start();

        return process;
    }

    static void RunServer(string[] args)
    {
        MineMP.ConsoleBuffer consoleBuffer;
        consoleBuffer = new();
        InitializeConsoleInterface(consoleBuffer);

        Server.ServerInConsole server;
        server = new Server.ServerInConsole(consoleBuffer, Globals.Config);
        server.Initialize();

        Process? supernodeProcess = null;
        Console.WriteLine("Launch supernode? [y/N]");
        switch (Console.ReadKey().Key)
        {
            case ConsoleKey.Y:
                {
                    supernodeProcess = StartSupernode();
                    break;
                }

            case ConsoleKey.N: break;
            default: break;
        }

        server.RunSync();

        if (supernodeProcess is not null)
        {
            supernodeProcess.Kill();
            supernodeProcess.WaitForExit();

            supernodeProcess.Close();
            supernodeProcess.Dispose();
        }
    }

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
        RunServer(args);
    }

    private static void ConsoleBuffer_ReadingInputLinePeeking(object sender)
    {
        return; //Ignore
    }

    private static void ConsoleBuffer_ReadingInputLine(object sender)
    {
        var input = Console.ReadLine();
        ((MineMP.ConsoleBuffer)sender).MakeInputLine(input ?? "");
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
