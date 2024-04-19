
# RuleForge

## Description


ML-Based Password-Mangling Rule Generator for Dictionary Attacks.

## Repository structure
````
.
├── MDBSCAN  
│   ├── MDBSCAN #source files for MDBSCAN and DBSCAN clustering
├── dictonaries               
│   ├── evolution #dictionaries used for evolution 
│   |   ├── 0_training 
│   |   ├── 1_attack 
│   |   ├── 2_target  
│   ├── experiments #dictionaries used for experiments 
└── README.md
````


## Usage: 
For clustering with HAC and Affinity propagation:
````
python3.9 rule_generator.py --wordlist <wordlist_file> --rulefile <rule_file> ( --hac | --ap )
````
For clustering with DBSCAN:
````
./MDBSCAN/MDBSCAN/bin/Release/net7.0/MDBSCAN <min_pts> <eps> <wordlist_file> | python3.9 rule_generator.py --rulefile <rule_file> --stdin
````
For clustering with MDBSCAN:
````
./MDBSCAN/MDBSCAN/bin/Release/net7.0/MDBSCAN <eps1> <eps2> <min_pts> <wordlist_file> | python3.9 rule_generator.py --rulefile <rule_file> --stdin
````

#### Example usage:

Generate top 100 rules from darkweb2017-top10k-m.txt using hac clustering method with distance_threshold parameter set to 2:
````
python3.9 rule_generator.py --wordlist dictionaries/experiments/darkweb2017-top10k-m.txt --rulefile darkweb_rules.rule --hac --distance_threshold 2 --most_frequent 100 
````
Generate rules from darkweb2017-top10k-m.txt using MDBSCAN clustering method with  parameters esp1 set to 1, esp2 set to 0.25 and min_pts set to 3:
````
./MDBSCAN/MDBSCAN/bin/Release/net7.0/MDBSCAN 1 0.25 3  dictionaries/experiments/darkweb2017-top10k-m.txt | python3.9 rule_generator.py --rulefile darkweb_rules.rule --stdin 
````

#### Options:
##### Required Arguments:
- `--wordlist <wordlist_file>`: Path to the wordlist file. This is the input wordlist for rule generation.
- `--rulefile <rule_file>`: Path to the output file where the generated rules will be saved.

##### Optional Arguments:
- `--rule_priority <rule_priority_file>`: Path to the priority rule file, for sorting or prioritizing rules.
- `--most_frequent <number_of_rules>`: Creates a file with the specified number of most frequent rules.
- `--distance_matrix_precomputed`: Use precomputed distance matrix
##### Clustering Algorithms Options:

- `DBSCAN`: 
  - `<eps>`: The maximum distance between two samples for one to be considered as in the neighborhood of the other. 
  - `<min_pts>`: The number of samples in a neighborhood for a point to be considered as a core point. 
 - `MDBSCAN`: 
	  - `<eps1>`: The maximum distance between two samples for one to be considered as in the neighborhood of the other. 
	  -  `<eps1>`: Jaro-winkler distance for refining clusters. 
	  - `<min_pts>`: The number of samples in a neighborhood for a point to be considered as a core point. 
- `--hac`: Use hierarchical agglomerative clustering algorithm.
  - `--distance_threshold <value>`: The linkage distance threshold at or above which clusters will not be merged.
- `--ap`: Use affinity propagation clustering algorithm.
  - `--damping <value>`: Damping factor between 0.5 and 1. 
  - `--convergence_iter <value>`: Number of iterations to wait for convergence.
  
## Scripts: 
- `bench_gen_times.py `: Benchmarking rule-generation time for MDBSCAN, DBSCAN, HAC and AP clustering methods
- `rule-priority-evolution.ipynb`: Rule priority evolution jupyter notebook
- `distance_matrix_generator.py:` Generates distance matrices from all .txt files in specified directory
