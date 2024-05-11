# BLE通信模块
import bluetooth
import time
import struct
from micropython import const

# BLE事件定义
_IRQ_CENTRAL_CONNECT = const(1)
_IRQ_CENTRAL_DISCONNECT = const(2)
_IRQ_GATTS_WRITE = const(3)
# BLE服务定义
_FLAG_READ = const(0x0002)
_FLAG_WRITE_NO_RESPONSE = const(0x0004)
_FLAG_WRITE = const(0x0008)
_FLAG_NOTIFY = const(0x0010)
# 定义支持的服务，0000xxxx-0000-1000-8000-00805f9b34fb为通用字符串
_UART_UUID = bluetooth.UUID("0000ffe0-0000-1000-8000-00805f9b34fb")
_UART_TX = (
    bluetooth.UUID("0000ffe7-0000-1000-8000-00805f9b34fb"),
    _FLAG_READ | _FLAG_NOTIFY,
)
_UART_RX = (
    bluetooth.UUID("0000ffe6-0000-1000-8000-00805f9b34fb"),
    _FLAG_WRITE | _FLAG_WRITE_NO_RESPONSE,
)
_UART_SERVICE = (
    _UART_UUID,
    (_UART_TX, _UART_RX),
)

class CONFIG_BLE:
    def __init__(self, name, connect_call, write_call):
        self._connection = None
        self._connect_callback = connect_call
        self._write_callback = write_call
        # 初始化BLE
        self._ble = bluetooth.BLE()
        self._ble.active(True)
        self._ble.irq(self._irq)
        ((self._handle_tx, self._handle_rx),) = self._ble.gatts_register_services((_UART_SERVICE,))
        # 开始广播
        self._advertise_data = self.advertising_content(name)
        self._advertise()
    
    # 获取蓝牙广播内容
    def advertising_content(self, name):
        payload = bytearray()
        # 添加固定数据头:struct.pack("BB", len(0x02+0x04) + 1, 0x01) + (0x02+0x04)
        # 详情参考:https://github.com/micropython/micropython/blob/master/examples/bluetooth/ble_advertising.py
        payload += b'\x02\x01\x06'
        # 添加蓝牙名称，格式参考数据头
        payload += struct.pack("BB", len(name) + 1, 0x09) + name
        return payload
    
    # BLE回调事件
    def _irq(self, event, data):
        if event == _IRQ_CENTRAL_CONNECT:
            # 开始连接
            self._connection, _, _ = data
            print("BLE New connection", self._connection)
            # 连接回调事件
            if self._connect_callback:
                self._connect_callback()
        elif event == _IRQ_CENTRAL_DISCONNECT:
            # 结束连接
            print("BLE Disconnected", self._connection)
            self._connection = None
            # 继续广播
            self._advertise()
        elif event == _IRQ_GATTS_WRITE:
            # 接收到消息
            conn_handle, value_handle = data
            value = self._ble.gatts_read(value_handle)
            if value_handle == self._handle_rx and self._write_callback:
                self._write_callback(value)
    
    # 发送消息事件
    def send(self, data):
        if self._connection != None:
            self._ble.gatts_notify(self._connection, self._handle_tx, data)

    # 返回当前是否已连接BLE
    def is_connected(self):
        return self._connection != None
    
    # 开始蓝牙广播事件
    def _advertise(self, interval_us=500000):
        print("BLE Starting advertising")
        self._ble.gap_advertise(interval_us, adv_data=self._advertise_data)

# 测试用例
if __name__ == "__main__":
    def on_msg_callback(v):
        print("RX", v)
        ble.send(b'config right')
    ble = CONFIG_BLE('epy-water-4012', on_msg_callback)
    while True:
        time.sleep_ms(100)
