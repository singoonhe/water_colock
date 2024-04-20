# ESP32-C3独立控制小车
from config import CONFIG
from screen import SCREEN
from machine import Pin
from time import sleep

# 温度更新倒计时
MEASURE_DTIME = const(60)
DRINK_DTIME = const(60)
NOTICE_DTIME = const(60)

class MAIN:
    # 初始化方法
    def __init__(self):
        # 读取配置文件
        self.config = CONFIG()
        self.measure_mtime = self.config.read_one('measure_time', MEASURE_DTIME)
        self.drink_mtime = self.config.read_one('drink_time', DRINK_DTIME)
        self.notice_mtime = self.config.read_one('notice_time', NOTICE_DTIME)
        # 人体
        self.scr = Pin(7)
        # 加载屏幕显示(屏幕、温度、电量)
        self.screen = SCREEN(2, 3, 10, 0)
        self.screen.update_measure()
        self.show_drink = False
        self.screen.refesh_screen(self.show_drink)
        # 倒计时控制
        self.measure_down_time = self.measure_mtime
        self.drink_down_time = self.drink_mtime
        self.notice_down_time = 0
        
    # 主循环方法
    def run(self):
        try:
            while True:
                sleep(1)
                # 判断温湿度更新
                self.measure_down_time -= 1
                if self.measure_down_time <= 0:
                    self.measure_down_time = self.measure_mtime
                    print('update measure')
                    self.screen.update_measure()
                    self.screen.refesh_screen(self.show_drink)
                # 提醒
                if self.notice_down_time > 0:
                    self.notice_down_time -= 1
                    if self.notice_down_time == 0:
                        self.show_drink = False
                    else:
                        self.show_drink = not self.show_drink
                    self.screen.refesh_screen(self.show_drink)
                # 判断喝水提醒更新
                self.drink_down_time -= 1
                if self.drink_down_time <= 0:
                    self.drink_down_time = self.drink_mtime
                    print('start notice')
                    self.notice_down_time = self.notice_mtime
        except KeyboardInterrupt:
            print('exit from interrupt')

if __name__ == '__main__':
    cur_main = MAIN()
    cur_main.run()

