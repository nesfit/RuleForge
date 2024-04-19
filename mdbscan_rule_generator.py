# #!/usr/bin/env python3

import argparse
import sys
import json
from operator import itemgetter
import os

import Levenshtein as lev


from collections import Counter


class RuleGenerator:
    def __init__(self):
        self.wordlist = None #input wordlist file
        self.rule_file = None #output rule file

        self.passwords = [] #passwors from wordlist file
        self.clusters = {}
        self.cluster_representatives = {}

        self.rules = []
        self.rules_priority = {}
        
        self.lambda_functions = {
            "TN": lambda x, y: x[:y] + x[y].swapcase() + x[y+1:],
            "zN": lambda x, y: x[0]*y+x,                      
            "ZN": lambda x, y: x+x[-1]*y,
            "sXY": lambda x, y, z: x.replace(y,z),
            "$X": lambda x, y: x + y,
            "^X": lambda x, y: y + x,
            "[" : lambda x: x[1:],
            "]" : lambda x: x[:-1],
            "DN": lambda x,y: x[:y]+x[y+1:],
            "iNX": lambda x,y,z: x[:y]+z+x[y:],
        }
    

    #Get input arguments
    def process_args(self):
        parser = argparse.ArgumentParser(prog = 'RuleGenerator')
        parser.add_argument('--wordlist', nargs=1)
        parser.add_argument('--rulefile', nargs=1, required=True)

        parser.add_argument('--stdin',action='store_true')
        parser.add_argument('--most_frequent', nargs=1)
        args = parser.parse_args()

        if args.wordlist:
            self.wordlist = args.wordlist[0]
        elif not args.stdin:
            print('You must select a wordfile.',file=sys.stderr)
            exit(1)

        self.rule_file = args.rulefile[0]
        self.STDIN = args.stdin
        self.most_frequent = int(args.most_frequent[0]) if args.most_frequent else None

        self.create_priority_dict_with_functions()
        self.external_clustering()
    
        
    def create_priority_dict_with_functions(self):
        #rules from priority groups 4-7
        rules = [
                "sXY",
                "zN",
                "ZN",
                "$X",
                "^X",
                "[",
                "]",
                "DN",
                "iNX"
            ]

        priority = 1
        for rule in rules:
            rule_name = rule.strip() 
            if rule_name in self.lambda_functions:
                self.rules_priority[rule_name] = {'priority': priority, 'func': self.lambda_functions[rule_name]}
                priority += 1
            else:
                print(f"Warning: No lambda function defined for rule '{rule_name}'")       

    #DBSCAN and MDBSCAN
    def external_clustering(self):
        self.clusters = json.load(sys.stdin)
        self.compute_cluster_representative_external()
        self.get_rules_from_cluster()
    
     #computation of cluster representative
    def compute_cluster_representative_external(self):
        for (label,cluster) in self.clusters.items():
            average_distances = []
            for entry in cluster:
                distances = []
                for other in cluster:
                    distances.append(self.edit_distance_calculator.compare(entry,other,max_distance=100))
                average_distances.append(sum(distances) / len(distances))
            representative, _ = min(enumerate(average_distances), key=itemgetter(1))
            self.cluster_representatives[label] = cluster[representative]

    def get_rules_from_cluster(self):
        for label, passwords_in_cluster in self.clusters.items():
            if label == -1:
                continue
            representative = self.cluster_representatives[label]

            cluster_rules = [] 

            for password in passwords_in_cluster:
                
                word_rules = self.generate_hashcat_rules(representative, password)
                cluster_rules.extend(word_rules)
            self.rules.extend(cluster_rules)
                    

        if (self.most_frequent == None):
            self.most_frequent=len(self.rules)

        self.save_frequent_rules_to_file() 
 

    def save_frequent_rules_to_file(self):
        counter = Counter()
        for rule in self.rules:
            counter[rule] += 1

        with open(self.rule_file , 'w', encoding='utf-8', errors='surrogateescape') as file:
            for rule, count in counter.most_common(self.most_frequent):
                file.write(f"{rule}\n")
        

    def int_to_hashcat(self,N):
        if N < 10:
            return str(N)
        else:
            return chr(65 + N - 10)




    def count_duplicate_first(self,password):
        count = 0
        for i in range(1, len(password)):
            if password[i] == password[0]:
                count += 1
            else:
                break
        return count

    def count_duplicate_last(self,password):
        count = 0
        for i in range(len(password) - 2, -1, -1):
            if password[i] == password[(len(password) - 1)]:
                count += 1
            else:
                break
        return count


    def find_applicable_rule_levenshtein(self, current, target):
        edit_operations_base = len(lev.editops(current, target))
        edit_operations = lev.editops(current, target)
        new_pass = current
        for rule, details in sorted(self.rules_priority.items(), key=lambda x: x[1]['priority']):

            func = details['func']
            arg_count = func.__code__.co_argcount

            if (arg_count == 2):
                if (rule == "DN"):
                        if (edit_operations[0][1] < len(current) and edit_operations[0][2] < len(target)):
                            new_pass = details['func'](current, edit_operations[0][1])
                            rule = rule.replace("N",self.int_to_hashcat(edit_operations[0][1]))
                elif (rule == "zN"):
                    if (edit_operations[0][1] < len(current) and edit_operations[0][2] < len(target)):
                        count_first = self.count_duplicate_first(target)
                        if (count_first > 1):
                            new_pass = details['func'](current, count_first)
                            rule = rule.replace("N",self.int_to_hashcat(count_first))
                            rule = rule.replace("X",target[edit_operations[0][1]])
                elif (rule == "ZN"):
                    if (edit_operations[0][1] < len(current) and edit_operations[0][2] < len(target)):
                        count_last = self.count_duplicate_last(target)
                        if (count_last > 1):
                            new_pass = details['func'](current, count_last)
                            rule = rule.replace("N", self.int_to_hashcat(count_last))
                            rule = rule.replace("X",target[edit_operations[0][1]])
                elif (rule == "^X"):
                    positions=[]
                    for op in edit_operations:
                        if (op[1] == 0):
                            positions.append(op)
                
                    if (len(positions) != 0):
                        last_operation = positions[-1] 
                        new_pass = details['func'](current, target[last_operation[2]])
                        rule = rule.replace("X",target[last_operation[2]])
                        
                else: 
                    if (edit_operations[0][2] == len(target)):
                        new_pass = details['func'](current, target[edit_operations[0][2]-1])
                        rule = rule.replace("N",self.int_to_hashcat(edit_operations[0][1]))
                        rule = rule.replace("X",target[edit_operations[0][2]-1])

                    else:
                        new_pass = details['func'](current, target[edit_operations[0][2]])
                        rule = rule.replace("N",self.int_to_hashcat(edit_operations[0][1]))
                        rule = rule.replace("X",target[edit_operations[0][1]])
            

            elif (arg_count == 3):
                if (edit_operations[0][1] < len(current) and edit_operations[0][2] < len(target)):
                    if (rule == "iNX"):
                        new_pass = details['func'](current, edit_operations[0][1], target[edit_operations[0][2]])
                        rule = rule.replace("X",target[edit_operations[0][1]])
                        rule = rule.replace("Y",target[edit_operations[0][2]])
                    else:
                        new_pass = details['func'](current, current[edit_operations[0][1]], target[edit_operations[0][2]])
                        rule = rule.replace("X",current[edit_operations[0][1]])
                        rule = rule.replace("Y",target[edit_operations[0][2]])
                rule = rule.replace("N",self.int_to_hashcat(edit_operations[0][1]))
            

            edit_operations_new = len(lev.editops(new_pass, target))
            if edit_operations_new < edit_operations_base:
                return rule, new_pass
            else:
                new_pass = current
                

    def generate_hashcat_rules(self, representant, password):
        generated_rules = []
        current_password = representant
        #generate rules from priority group 1
        if (current_password == password):
            generated_rules.append(":")

        #generate rules from priority groups 2-3
        #rule l
        elif (current_password.lower() == password):
            generated_rules.append("l")
        #rule u
        elif (current_password.upper() == password):
            generated_rules.append("u")
        #rule c
        elif ((current_password.lower()).capitalize() == password):
            generated_rules.append("c")
        #rule TN
        else:
            for i in range(0,min(len(current_password)-1,len(password)-1)):
                
                if (((current_password.lower())[i].swapcase() == password[i]) and (current_password[i].isalpha() == True)):
                    generated_rules.append(f"T{i}")


        #generate rules from priority groups 4-5
        while ((current_password.lower()) != (password.lower())):
            result = self.find_applicable_rule_levenshtein(current_password.lower(), password.lower())
            if result is not None:
                rule, new_password = result
                if rule:
                    generated_rules.append(rule)
                    current_password = new_password
            else:
                break
        

        single_rule = ' '.join(generated_rules)
        generated_rules.append(single_rule)
        return generated_rules



if __name__ == "__main__":
    ruleGenerator = RuleGenerator()
    ruleGenerator.process_args()

