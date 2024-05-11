# ESP32-C3独立控制小车
import time
import json
import machine
from config import CONFIG
from config_ble import CONFIG_BLE

class MAIN:
    # 初始化方法
    def __init__(self):
        # 读取配置文件
        self.config = CONFIG()
        # 初始化ble设置并开始广播
        self.ble = CONFIG_BLE('epy-water-4012', self.on_ble_connected, self.on_ble_msg)
    
    # 蓝牙消息回调
    def on_ble_msg(self, msg):
        print(msg)
        msg_dic = json.loads(msg.decode('utf-8'))
        if msg_dic.Cmd == 'Set':
            # 配置重置事件，保存到本地
            del msg_dic['Cmd']
            self.config.save(msg_dic)
        elif msg_dic.Cmd == 'Rebot':
            # 重启方法
            machine.reset()
    
    # 蓝牙连接回调事件
    def on_ble_connected(self):
        # 获取配置事件，回传当前的配置项
        self.ble.send(self.config.read_str().encode('utf-8'))
        
    # 主循环方法
    def run(self):
        try:
            while True:
                time.sleep_ms(100)
        except KeyboardInterrupt:
            print("Program stopped.")

if __name__ == '__main__':
    cur_main = MAIN()
    cur_main.run()

