import subprocess
import time
import resource
import sys

start = time.time()
for command in sys.argv[1:]:
    subprocess.run(command,shell=True)
end = time.time()
resource_usage = resource.getrusage(resource.RUSAGE_CHILDREN)
time_it_took = end - start
print(f'{time_it_took},{resource_usage.ru_maxrss}',end='')
