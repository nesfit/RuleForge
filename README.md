
# RuleForge

## Description

This repository contains RuleForge, a ML-Based Password-Mangling Rule Generator for Dictionary Attacks. It also includes additional scripts and dictionaries for experimental evaluation and a Jupyter Notebook for fine-tuning rule priorities with an evolutionary algorithm.

## Repository structure
````
.
├── MDBSCAN  
│   ├── MDBSCAN # Source files for MDBSCAN and DBSCAN clustering
├── dictonaries               
│   ├── evolution # Dictionaries used for evolution 
│   |   ├── 0_training 
│   |   ├── 1_attack 
│   |   ├── 2_target  
│   └── experiments # Dictionaries used for experiments 
├── README.md
├── RuleForge.py
├── bench_gen_times.py
├── distance_matrix_generator.py
└── rule-priority-evolution.ipynb
````


## Usage: 
For clustering with HAC and Affinity propagation:
````
python3.9 RuleForge.py --wordlist <wordlist_file> --rulefile <rule_file> ( --hac | --ap )
````
For clustering with DBSCAN:
````
./MDBSCAN/MDBSCAN/bin/Release/net7.0/MDBSCAN <min_pts> <eps> <wordlist_file> | python3.9 RuleForge.py --rulefile <rule_file> --stdin
````
For clustering with MDBSCAN:
````
./MDBSCAN/MDBSCAN/bin/Release/net7.0/MDBSCAN <eps1> <eps2> <min_pts> <wordlist_file> | python3.9 RuleForge.py --rulefile <rule_file> --stdin
````

#### Example usage:

Generate top 100 rules from darkweb2017-top10k-m.txt using hac clustering method with distance_threshold parameter set to 2:
````
python3.9 RuleForge.py --wordlist dictionaries/experiments/darkweb2017-top10k-m.txt --rulefile darkweb_rules.rule --hac --distance_threshold 2 --most_frequent 100 
````
Generate rules from darkweb2017-top10k-m.txt using MDBSCAN clustering method with  parameters esp1 set to 1, esp2 set to 0.25 and min_pts set to 3:
````
./MDBSCAN/MDBSCAN/bin/Release/net7.0/MDBSCAN 1 0.25 3  dictionaries/experiments/darkweb2017-top10k-m.txt | python3.9 RuleForge.py --rulefile darkweb_rules.rule --stdin 
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
  
## Additional Scripts: 
- `bench_gen_times.py `: Benchmarking rule-generation time for MDBSCAN, DBSCAN, HAC and AP clustering methods
- `rule-priority-evolution.ipynb`: Rule priority evolution jupyter notebook
- `distance_matrix_generator.py:` Generates distance matrices from all .txt files in specified directory


## Licensing information
RuleForge and related sripts are distributed under the MIT license (see License.txt for more information.).
The authors of these codes are Radek Hranicky, Lucia Sirova, Viktor Rucky from Brno University of Technology, Faculty of Information Technolgy, Czech Republic.

Note:
- The MDBSCAN clustering was implemented, to the best of our efforts, according to the research paper: Li, Shunbin, et al. "Mangling rules generation with density-based clustering for password guessing." IEEE Transactions on Dependable and Secure Computing (2022).
- The implementations of SymSpell fuzzy hashing, Jaro-Winkler distance calculation, and files inside `MDBSCAN/MDBSCAN/SoftWx.Match` have been adopted from their authors who also released them under the MIT License. Licensing details are available in the files' headers.
