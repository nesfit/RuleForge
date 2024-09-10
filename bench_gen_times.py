import sys
import os
import subprocess
import time
import pathlib

file = sys.stdin if len(sys.argv) == 1 else open(sys.argv[1])

methods : list[tuple[list[str],str]] = [
    (["./MDBSCAN/MDBSCAN/bin/Release/net7.0/MDBSCAN 1 3 {} | python3.9 RuleForge.py --stdin --rulefile {} --remove_outlier --representative combo"],
     "DBSCAN"),
    (["./MDBSCAN/MDBSCAN/bin/Release/net7.0/MDBSCAN 2 0.25 3 {} | python3.9 RuleForge.py --stdin --rulefile {} --remove_outlier --representative combo"],
     "MDBSCAN"),
    (["./MDBSCAN/DistanceMatrixGenerator/bin/Release/net7.0/DistanceMatrixGenerator {}", "python3.9 RuleForge.py --distance_matrix_precomputed --representative combo --wordlist {} --hac --distance_threshold 3 --rulefile {}"],
     "HAC"),
    (["./MDBSCAN/DistanceMatrixGenerator/bin/Release/net7.0/DistanceMatrixGenerator {}", "python3.9 RuleForge.py --distance_matrix_precomputed --representative combo --wordlist {} --ap --convergence_iter 15 --damping 0.7 --rulefile {}"],
     "AP")
]

wordlists : list[pathlib.Path]= []
while (line := file.readline().strip()) != '':
    if not os.path.isfile(line):
        print(f'{line} is not a file!',file=sys.stderr)
        exit(-1)
    wordlists.append(pathlib.Path(line))


print(f'rule_file,method,time,maximum resident set size')

for (methods,method_name) in methods:
    for wordlist in wordlists:
        for x in [wordlist.with_name(x) for x in os.listdir(wordlist.parent) if x.startswith('.MDBSCANcache') or x.endswith('_distance_matrix.npy')]:
            x.unlink()
        processed_methods = []
        for method in methods:
            processed_methods.append(f'"{method.format(wordlist,wordlist.with_name("rule"+method_name+wordlist.name))}"')
        result = subprocess.run(f'python3.9 bench_rules_child.py {" ".join(processed_methods)}',shell=True,capture_output=True)
        print(f'{wordlist},{method_name},{result.stdout.decode()}')
        for x in [wordlist.with_name(x) for x in os.listdir(wordlist.parent) if x.startswith('.MDBSCANcache') or x.endswith('_distance_matrix.npy')]:
            x.unlink()
        time.sleep(30)

file.close()
