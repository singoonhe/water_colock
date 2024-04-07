# ESP32-C3独立控制小车
from config import CONFIG 

class MAIN:
    # 初始化方法
    def __init__(self):
        # 读取配置文件
        self.config = CONFIG()
        
    # 主循环方法
    def run(self):
        pass

if __name__ == '__main__':
    cur_main = MAIN()
    cur_main.run()

