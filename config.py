# json配置文件的读取和保存
import json

class CONFIG:
    # 配置文件名，可不传
    def __init__(self, config_name = 'config.txt'):
        self.config_file = config_name
        # 如文件存在，则读取该配置
        self.config_dic = {}
        # 读取配置文件
        try:
            file = open(self.config_file, 'r')
            content = file.read()
            self.config_dic = json.loads(content)
            file.close()
        except OSError:  # open failed
            self.save_one('boot_web', 0)
            
    # 获取配置信息
    def read_one(self, key):
        return self.config_dic.get(key)

    # 保存部分配置
    def save_one(self, key, value):
        if isinstance(key, str) and len(key) > 1 and value != None:
            self.save({key: value})
            
    # 获取配置信息
    def read(self):
        return self.config_dic
            
    # 保存部分配置
    def save(self, params):
        if params == None or not isinstance(params, dict):
            return
        self.config_dic.update(params)
        # 保存到文件
        try:
            file = open(self.config_file, 'w')
            file.write(json.dumps(self.config_dic))
            file.close()
        except OSError:  # open failed
            print("Save config failed!")
        