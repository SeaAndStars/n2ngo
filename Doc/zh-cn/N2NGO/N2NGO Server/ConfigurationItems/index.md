# N2N GO Server 配置项

*[索引](../../../index.md) - [N2N GO](../../index.md) - [N2N GO Server](../index.md) - [配置](./index.md)*

---

适用于 **N2N GO Server** 的配置项目

**N2N GO Server** 会将配置加载至 **Server In Console** 的 `ServerConfig` 中。

## 可用配置项

**N2N GO Server** 的配置具有以下项目：

* #### `IPv4`

  *默认值:* `0.0.0.0`

  *释义:* 监听的**IPv4**


* #### `PortV4`

  *默认值:* `7476`

  *释义:* 监听的**IPv4**端口


* #### `IPv6`

  *默认值:* `::`

  *释义:* 监听的**IPv6**


* #### `PortV6`

  *默认值:* `7476`

  *释义:* 监听的**IPv6**端口

  > **备注：** <br>
  > 如果**IPv6**的监听端口和**IPv4**的监听端口相同，您必须提前确认您的目标平台是否支持IPv6双栈功能，否则将会导致服务器运行失败
  >


* #### `EnableIPv6`

  *默认值:* `1`

  *释义:* <br>
  启用**IPv6**监听<br>
  `0` - 不启用<br>
  任意值 - 启用
