# N2N GO Server 控制台命令

*[索引](../../../index.md) - [N2N GO](../../index.md) - [N2N GO Server](../index.md) - [控制台命令](./index.md)*

---

N2N GO Server 适用于 Server In Console 的控制台命令

## 控制台命令行

一个[**命令行**](#控制台命令行)由一个[**命令执行**](#命令执行)组成。

控制台每次的[**命令行**](#控制台命令行)可以执行一个[**命令**](#命令)。

### 命令执行
一个[**命令执行**](#命令执行)包含[**命令**](#命令)和**实参**。

一个[**命令执行**](#命令执行)会通过**空格**来分割为多个段，并且将**第一个段**视为[**命令**](#命令)；<br>
若干个被分割的段将会作为作为一个参数集合供命令读取。
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

## 控制台命令行内部处理

**N2N GO Server** 的 **Server In Console** 对**命令行**的命令名处理都会转换成小写，因此**命令名大小写不敏感**。<br>
**N2N GO Server** 的 **Server In Console** 对**命令行**的命令参数没有特殊处理，具体取决于上下文中的命令其处理方式，因此**参数可能需要注意大小写**。

当一个命令行向**形参列表**为**空**的命令传入参数，命令会忽略其得到的实参。

## 命令

**N2N GO Server** 在 **Server In Console** 初始化并运行后可以在控制台交互中使用以下命令：

* #### `clrscr`

  *释义:* 清空控制台屏幕输出。

  *参数:* 空

  > **备注：** <br>
  > 会向 `N2NGOServer` 的 `ConsoleBuffer` 发送 `ConsoleBuffer.ControlSymbols.ClearScreen` 控制符。
  >


* #### `stop`

  *释义:* 断开所有连接并关闭 **N2N GO Server**。

  *参数:* 空


* #### `list`

  *释义:* <br>
  列出 **N2N GO Server** 中的有效房间；<br>
  当 `ROOM_CODE` 参数未提供时，将会列出所有有效的房间。

  *参数:* 
  * `ROOM_CODE`[,...]
    
    *是否可选:* 可选

    *默认值:* 空

    *释义:* 指定一个或多个将要列出房间详细信息的房间号。


* #### `list_tcp`

  *释义:* 列出所有已建立连接的TCP客户端及详细信息。

  *参数:* 空

  > **备注：** <br>
  > 此命令不会过滤所有客户端，因此一些已建立的非 N2N GO 客户端的不明客户端连接可能会被展现，<br>
  > 建立的连接时间长、不发送或接受任何应用包、连接，通常这类连接的特性，<br>
  > 我们将这类连接视为恶意僵尸网络活动，并且正在制定更新计划来使 **N2N GO Server** 自动反制这些烦人的恶意连接。<br>
  > <br>
  > 目前可以使用 [*disconnect*](#disconnect) 命令来手动结束您觉得可疑的连接。
  >


* #### `close`

  *释义:* <br>
  关闭 **N2N GO Server** 中的房间；<br>
  当 `ROOM_CODE` 参数未提供时，将会返回。

  *参数:* 
  * `ROOM_CODE`[,...]
    
    *是否可选:* 必须

    *释义:* 指定一个或多个将要被关闭的房间的房间号。


* #### `close_all`
  
  *释义:* 关闭 **N2N GO Server** 中的所有房间。

  *参数:* 空


* #### `create`

  *释义:* <br>
  在 **N2N GO Server** 中创建房间；<br>
  如果参数 `ROOM_CODE` 或 `ROOM_NAME` 未提供时将会返回。

  *参数:* 

  * `ROOM_CODE`
    
    *是否可选:* 必须

    *释义:* 指定房间号。

  * `ROOM_NAME`
    
    *是否可选:* 必须

    *释义:* 指定房间名称。
    
  * `ROOM_VISIBILITY`
    
    *是否可选:* 可选

    *默认值:* `0`

    *释义:* <br>
    指定房间可见性。<br>
    `0` - 公开<br>
    `1` - 隐藏
    
  * `ROOM_NEEDS_PASSWORD`
    
    *是否可选:* 可选

    *默认值:* `0`

    *释义:* <br>
    指定房间是否需要密码保护。<br>
    `0` - 不需要<br>
    `1` - 需要
    
  * `ROOM_PASSWORD`
    
    *是否可选:* 可选

    *默认值:* `null`

    *释义:* 指定房间密码。

    > **备注：** <br>
    > 我们默认使用 '`null`' 作为公开房间的默认密码<br>
    >
    
  * `ROOM_COLOR_MAIN`
    
    *是否可选:* 可选

    *默认值:* `0x00000000`

    *释义:* 指定房间主要颜色。
    
  * `ROOM_COLOR_MINOR`
    
    *是否可选:* 可选

    *默认值:* `0x00000000`

    *释义:* 指定房间次要颜色。


* #### `save_rooms`
  
  *释义:* 导出 **N2N GO Server** 中的所有房间至磁盘中。

  *参数:* 空


* #### `load_rooms`
  
  *释义:* 导入上次保存在磁盘上的所有房间至 **N2N GO Server**。

  *参数:* 空


* #### `disconnect`

  *释义:* <br>
  断开 **N2N GO Server** 中已建立的Tcp连接；<br>
  当 `INDEX` 参数未提供时，将会断开所有连接。

  *参数:* 
  * `INDEX`[,...]
    
    *是否可选:* 可选

    *默认值:* 空

    *释义:* 指定一个或多个将要断开的连接的**索引**。

    > **备注：** <br>
    > 可以通过使用 [*list_tcp*](#list_tcp) 命令来获取Tcp客户端连接的索引
    >
  
    *选项:*

    * `-y`, `--no-ask` 不要询问
