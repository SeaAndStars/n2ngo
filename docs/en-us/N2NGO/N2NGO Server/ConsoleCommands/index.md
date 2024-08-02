# N2N GO Server Commands

*[Index](../../../index.md) - [N2N GO](../../index.md) - [N2N GO Server](../index.md) - [Commands](./index.md)*

---

> **Note:** Portions of this page are machine translated

Commands for Server In Console

## Command Line

A [**Command Line**](#command-line) consists of a [**Command Execution**](#command-execution).  
  
Each [**Command Line**](#command-line) entered in the console can execute a single [**Command**](#command).  
  
### Command Execution  
A [**Command Execution**](#command-execution) includes a [**Command**](#command) and **arguments**.  
  
A [**Command Execution**](#command-execution) is split into multiple segments by **spaces**, with the **first segment** considered as the [**Command**](#command);<br>  
The remaining segmented parts are used as a set of arguments for the command to read.
``` csharp
// -- Command Arguments Example --

// *User Input: cmd arg1 arg2

// Command Controller
bool OnCommandExectued(object? sender = null, ReadOnlyCollection<string> args)
{
    foreach (var arg in args)
        Log.OutInfo($"{arg}, ");
}

// *Start executing command
var invokingResult = OnCommandExectued(args: args);

// --- Log Output ---
cmd, arg1, arg2, 
```

## Internal command handling

**N2N GO Server**'s **Server In Console** processes command names in **command lines** by converting them to lowercase, therefore **command name case insensitivity** applies.<br>  
**N2N GO Server**'s **Server In Console** does not apply special treatment to the command parameters in **command lines**; the handling of these parameters is specific to the command context, hence **parameters may need to be case-sensitive**.  
  
When a command line passes arguments to a command that has an **empty** **formal parameter list**, the command will ignore the received actual parameters.

## Commands
# N2N GO Server Console Commands  
  
After initializing and running **N2N GO Server** in **Server In Console** mode, the following commands can be used for interaction in the console:  
  
## `clrscr`  
  
- **Description**: Clears the console screen output.  
- **Parameters**: None  
- **Note**: Sends the `ConsoleBuffer.ControlSymbols.ClearScreen` control symbol to the `N2NGOServer`'s `ConsoleBuffer`.  
  
## `stop`  
  
- **Description**: Disconnects all connections and shuts down **N2N GO Server**.  
- **Parameters**: None  
  
## `list`  
  
- **Description**: Lists valid rooms in **N2N GO Server**; if no `ROOM_CODE` parameter is provided, it lists all valid rooms.  
- **Parameters**:  
  - `ROOM_CODE`[,...]  
    - **Optional**: Yes  
    - **Default**: None  
    - **Description**: Specifies one or more room codes for which to list detailed room information.  
  
## `list_tcp`  
  
- **Description**: Lists all established TCP client connections and their details.  
- **Parameters**: None  
- **Note**: This command does not filter out all clients, so some non-N2N GO client connections that are established but unknown may be shown. Established connections that are old, do not send or receive any application packets, and exhibit characteristics of potentially malicious botnet activity are being monitored, and plans are underway to update **N2N GO Server** to automatically counter such nuisance connections. Currently, you can use the [*disconnect*](#disconnect) (hypothetical command) to manually end suspected connections.  
  
---  
  
**Note**: The `disconnect` command is hypothetical and not explicitly mentioned in your original request. However, based on the need for managing TCP connections, I have included it as a hypothetical addition to demonstrate how such a command might be structured in a Markdown list.

## `close`  
  
  * **Description**: Closes one or more rooms in **N2N GO Server**; if no `ROOM_CODE` is provided, the command will return without action.  
  
  * **Parameters**:  
    - `ROOM_CODE`[,...]  
      - **Required**: Yes  
      - **Description**: Specifies one or more room codes of the rooms to be closed.  
  
## `close_all`  
  
  * **Description**: Closes all rooms in **N2N GO Server**.  
  
  * **Parameters**: None  
  
## `create`  
  
  * **Description**: Creates a new room in **N2N GO Server**; if `ROOM_CODE` or `ROOM_NAME` is not provided, the command will return without action.  
  
  * **Parameters**:  
    - `ROOM_CODE`  
      - **Required**: Yes  
      - **Description**: Specifies the room code.  
    - `ROOM_NAME`  
      - **Required**: Yes  
      - **Description**: Specifies the room name.  
    - `ROOM_VISIBILITY`  
      - **Optional**: Yes  
      - **Default**: `0`  
      - **Description**: Specifies the room visibility. `0` - Public, `1` - Hidden.  
    - `ROOM_NEEDS_PASSWORD`  
      - **Optional**: Yes  
      - **Default**: `0`  
      - **Description**: Specifies whether the room requires a password. `0` - No, `1` - Yes.  
    - `ROOM_PASSWORD`  
      - **Optional**: Yes  
      - **Default**: `null`  
      - **Description**: Specifies the room password.  
      > **Note**: We default to `null` as the password for public rooms.  
    - `ROOM_COLOR_MAIN`  
      - **Optional**: Yes  
      - **Default**: `0x00000000`  
      - **Description**: Specifies the main color of the room.  
    - `ROOM_COLOR_MINOR`  
      - **Optional**: Yes  
      - **Default**: `0x00000000`  
      - **Description**: Specifies the secondary color of the room.  
  
## `save_rooms`  
  
  * **Description**: Exports all rooms in **N2N GO Server** to disk.  
  
  * **Parameters**: None  
  
## `load_rooms`  
  
  * **Description**: Imports all rooms previously saved on disk into **N2N GO Server**.  
  
  * **Parameters**: None  
  
## `disconnect`  
  
  * **Description**: Disconnects one or more established TCP connections in **N2N GO Server**; if no `INDEX` is provided, all connections will be disconnected.  
  
  * **Parameters**:  
    - `INDEX`[,...]  
      - **Optional**: Yes  
      - **Default**: None  
      - **Description**: Specifies one or more indices of the connections to be disconnected.  
      > **Note**: You can obtain the indices of TCP client connections by using the [*list_tcp*](#list_tcp) command.  
  
    * **Options**:  
      - `-y`, `--no-ask`: Do not prompt for confirmation before disconnecting connections.
  