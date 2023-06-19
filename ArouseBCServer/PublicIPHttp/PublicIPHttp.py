import os
from sanic import Sanic, text
from sanic.handlers import ErrorHandler
import multiprocessing
from datetime import datetime

class CustomErrorHandler(ErrorHandler):
    def default(self, request, exception):
        # You custom error handling logic...
        print('未知错误 %s' %(exception))
        return super().default(request, exception)


app = Sanic("ArouseBlockchainTools",error_handler=CustomErrorHandler)
app.config.REAL_IP_HEADER = 'X-Real-IP'
app.config.KEEP_ALIVE = False
app.config.FORWARDED_FOR_HEADER = 'X-Forwarded-For'
app.config.PROXIES_COUNT = 2
app.config.GRACEFUL_SHUTDOWN_TIMEOUT = 10.0

@app.get("/")
async def handler(request):
    return text('null')

@app.get("/Pip")
async def public_ip_handler(request):
    try:
        return text(str(request.ip)
        )
    except ValueError:
        print('输入的值不是合法的整数')
        return text('输入的值不是合法的整数')
    except Exception as r:
        print('未知错误 %s' %(r))
        return text('未知错误 %s' %(r))

confg_file_mtime = datetime(1998, 2, 28, 12, 35, 9)
config_json = None
@app.get("/cj")
async def config_json_handler(request):
    try:
        global confg_file_mtime
        global config_json
        
        confgFile = os.path.dirname(__file__) + '/Config/appsetting.json'  
        stinfo = os.stat(confgFile)
        mtime_string = datetime.fromtimestamp(int(stinfo.st_mtime))
        if confg_file_mtime != mtime_string:
            with open(confgFile,encoding="utf-8") as f:
                config_json = f.read()
                print('文件' + confgFile +'修改时间' + str(mtime_string) + " : " +config_json)
                confg_file_mtime = mtime_string

        if config_json != None and len(config_json) != 0:
            return text(config_json)
        else:
            confg_file_mtime = datetime(1998, 2, 28, 12, 35, 9)
            return text('未知错误，没有读取到Json文件')

    except ValueError:
        print('输入的值不是合法的整数')
        return text('输入的值不是合法的整数')
    except Exception as r:
        print('未知错误 %s' %(r))
        return text('未知错误 %s' %(r))
    

cpu_count = multiprocessing.cpu_count()
min_workers = min([cpu_count,2])

if __name__ == "__main__":
    app.run(host='0.0.0.0', port=43023,debug=True,workers=min_workers, access_log=False)
 


# 本地浏览器访问：  http://localhost:43023
# 公网访问地址：    http://120.26.66.97:43023

#1. 使用Python项目管理器来进行部署
