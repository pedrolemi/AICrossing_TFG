from pathlib import Path
from jsonschema import SchemaError
from jsonschema.protocols import Validator
import json

DATA_DIRECTORY = Path("data")

# Datos necesarios para generar las misiones
QUESTS_DIRECTORY = DATA_DIRECTORY / "quests"

SCHEMAS_DIRECTORY = QUESTS_DIRECTORY / "schemas"
SCHEMAS_TEMPLATES_DIRECTORY = SCHEMAS_DIRECTORY / "templates"
SCHEMAS_TEMPLATES_BASES_DIRECTORY = SCHEMAS_TEMPLATES_DIRECTORY / "bases"
SCHEMAS_SUMMARIES_DIRECTORY = SCHEMAS_DIRECTORY / "summaries"

QUESTS_SUMMARIES_DIRECTORY = QUESTS_DIRECTORY / "summaries"
QUESTS_DESCRIPTIONS_DIRECTORY = QUESTS_DIRECTORY / "descriptions"

DATASETS_DIRECTORY = DATA_DIRECTORY / "datasets"
FIRST_NAMES_FILE = DATASETS_DIRECTORY / "first_names.txt"
STARDEW_VALLEY_QUESTS_FILE = DATASETS_DIRECTORY / "stardew_valley_quests.txt"

# Informacion acerca del juego
GAME_INFO_DIRECTORY = DATA_DIRECTORY / "game_info"
RELATIONSHIPS_FILE = GAME_INFO_DIRECTORY / "relationships.txt"
DESCRIPTIONS_DIRECTORY = GAME_INFO_DIRECTORY / "descriptions"
ITEMS_DIRECTORY = GAME_INFO_DIRECTORY / "items"
REGULAR_ITEMS_FILE = ITEMS_DIRECTORY / "regular_items.txt"
LOST_ITEMS_FILE = ITEMS_DIRECTORY / "lost_items.txt"

# Extensiones que se usan
JSON_EXTENSION = ".json"
TXT_EXTENSION = ".txt"

WILDCARD_CHARACTER = '*'
JSON_EXTENSION_GLOB = WILDCARD_CHARACTER + JSON_EXTENSION
TXT_EXTENSION_GLOB = WILDCARD_CHARACTER + TXT_EXTENSION

##################################
######## METODOS COMUNES #########
##################################

def load_json(path):
    with open(path, 'r') as f:
        json_object = json.load(f)
    return json_object

def load_json_DIRECTORY(path):
    jsons = {}
    for file in path.glob(JSON_EXTENSION_GLOB):
        json = load_json(file)
        filename = file.stem
        jsons[filename] = json
    return jsons

def load_txt_dataset(path):
    with open(path, 'r') as f:
        txt_dataset = f.read()
        aux = []
        for txt in txt_dataset.splitlines():
            if txt.strip():
                aux.append(txt)
        aux = set(aux)
    return list(aux)

def load_txt_directory(path):
    infos = {}
    for file in path.glob(TXT_EXTENSION_GLOB):
        info = file.read_text()
        filename = file.stem
        infos[filename] = info
    return infos

def load_items(path):
    items = {}
    with open(path, 'r') as f:
        for line in f:
            sections = line.split(':')
            value = int(sections[0])
            items[value] = (sections[1].strip(), sections[2].strip())
    return items

##################################
##### INFORMACION DE JUEGO #######
##################################

def load_relationships():
    with open(RELATIONSHIPS_FILE, 'r') as f:
        content = f.read()
        sections = content.split('\n\n')

        typesAux = sections[0].split('\n')
        types = {}
        for type in typesAux:
            typesValues = type.split(':')
            value = (int)(typesValues[0])
            types[value] = typesValues[1].strip()

        weights = {}
        relationships = sections[1].split('\n')
        for relation in relationships:
            relation = relation.split()
            characters = (relation[0], relation[1])
            value = int(relation[2])

            if not value in weights:
                weights[value] = []
            
            weights[value].append(characters)

        return weights, types
    
def load_descriptions():
    return load_txt_directory(DESCRIPTIONS_DIRECTORY)

def load_regular_items():
    return load_items(REGULAR_ITEMS_FILE)

def load_lost_items():
    return load_items(LOST_ITEMS_FILE)
    
##################################
########### DATASETS #############
##################################

# Dataset de nombres
# https://github.com/dominictarr/random-name/blob/master/first-names.txt
def load_first_names():
    return load_txt_dataset(FIRST_NAMES_FILE)

# Misiones de stardew valley, que se usan como referencia de estilo
# https://es.stardewvalleywiki.com/Misiones
def load_stardew_valley_quests():
    return load_txt_dataset(STARDEW_VALLEY_QUESTS_FILE)

##################################
########### ESQUEMAS #############
##################################

def load_schemas(path):
    schemas = load_json_DIRECTORY(path)

    for name, schema in schemas.items():
        try:
            Validator.check_schema(schema)
        except SchemaError as e:
            print(f"Wrong schema: {e.message()}")
            del schemas[name]

    return schemas

# Se usan para validar y encontrar el tipo de mision
def load_quests_templates_schemas():
    return load_schemas(SCHEMAS_TEMPLATES_DIRECTORY)
def load_quests_templates_bases_schemas():
    return load_schemas(SCHEMAS_TEMPLATES_BASES_DIRECTORY)

# Se usan para obtener los campos necesarios para crear posteriormente una mision de ejemplo
def load_quests_summaries_schemas():
    return load_schemas(SCHEMAS_SUMMARIES_DIRECTORY)

##################################
########### PLANTILLAS ###########
##################################
    
# Se usan para obtener los campos necesarios para crear posteriormente una mision de ejemplo
def load_quests_summaries():
    return load_txt_directory(QUESTS_SUMMARIES_DIRECTORY)

# Plantillas de las misiones
def load_quests_descriptions():
    return load_txt_directory(QUESTS_DESCRIPTIONS_DIRECTORY)

##################################

class QuestLoader():
    def __init__(self):
        self.input = Path("input")
        self.input.mkdir(parents=True, exist_ok=True)
        
        self.output = Path("output")
        self.output.mkdir(parents=True, exist_ok=True)

        self.quests = {}

    def quest_exists(self, name, quest):
        quest_giver_name = quest["quest_giver_name"]
        name = name + JSON_EXTENSION
        path = self.output / quest_giver_name / name
        return path.exists()

    def load_quests(self):
        return load_json_DIRECTORY(self.input)
    
    def add_quest(self, name, quest, description, title, readability):
        quest = quest.copy()
        quest["description"] = description
        quest["title"] = title
        quest["readability"] = readability
        del quest["topic"]

        quest_giver_name = quest["quest_giver_name"]
        
        if not quest_giver_name in self.quests:
            self.quests[quest_giver_name] = []
        self.quests[quest_giver_name].append((name, quest))

    def write_quests(self):
        for quest_giver_name, quests in self.quests.items():
            directory_path = self.output / quest_giver_name
            directory_path.mkdir(parents=True, exist_ok=True)

            for quest in quests:
                name = quest[0]
                quest = quest[1]

                name = name + JSON_EXTENSION
                path = directory_path / name
            
                with open(path, 'w', encoding='utf-8') as f:
                    json.dump(quest, f, indent=4, ensure_ascii=False)