# Takes stdin or file as input
# Input is a series of paths to wordlists each on a separate line
# Output is time it took to produce rules per method on each wordlist
# All methods are run without loaded pre-cached results

import sys
import os
import subprocess
import time
import pathlib

file = sys.stdin if len(sys.argv) == 1 else open(sys.argv[1])

methods : list[tuple[str,str]] = [
    ("./MDBSCAN/MDBSCAN/bin/Release/net7.0/MDBSCAN 1 3 {} | python3.9 rule_generator.py --stdin --rulefile {} --remove_outlier","DBSCAN"),
    ("./MDBSCAN/MDBSCAN/bin/Release/net7.0/MDBSCAN 2 0.25 3 {} | python3.9 rule_generator.py --stdin --rulefile {} --remove_outlier","MDBSCAN"),
    ("python3.9 RuleForge.py --wordlist {} --hac --distance_threshold 3 --rulefile {}","HAC"),
    ("python3.9 RuleForge.py --wordlist {} --ap --convergence_iter 15 --damping 0.7 --rulefile {}","AP")
]

wordlists : list[pathlib.Path]= []
while (line := file.readline().strip()) != '':
    if not os.path.isfile(line):
        print(f'{line} is not a file!',file=sys.stderr)
        exit(-1)
    wordlists.append(pathlib.Path(line))


print('rule_file,method,time')
for wordlist in wordlists:
    for (method,method_name) in methods:
            start = time.time()
            result = subprocess.run(method.format(wordlist,wordlist.with_name('rule'+method_name+wordlist.name)),shell=True,stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)
            end = time.time()
            time_it_took = end - start
            for x in [wordlist.with_name(x) for x in os.listdir(wordlist.parent) if x.startswith('.MDBSCANcache')]:
                 x.unlink()
            print(f'{wordlist},{method_name},{time_it_took}')

file.close()
