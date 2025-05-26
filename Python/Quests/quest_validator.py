from referencing import Registry, Resource
from jsonschema import Draft202012Validator
import loader
import relationships

# Clase que usa jsonschemas para determinar el tipo de mision
class QuestValidator():
    def __init__(self):
        # Se obtiene todos los esquemas bases
        bases_schemas = loader.load_quests_templates_bases_schemas()

        registry = Registry()

        for schema in bases_schemas.values():
            resource = Resource.from_contents(schema)
            registry = resource @ registry
            
        self.validator = Draft202012Validator(schema = {}, registry = registry)

        # Se obtienen cada uno de los esquemas, que correspnde con un tipo de mision
        self.quests_schemas = loader.load_quests_templates_schemas()

    def find_quest_type(self, quest):
        quest_type = None
        # Se comprueba con cada esquema, para determinar el tipo de mision
        for name, schema in self.quests_schemas.items():
            self.validator = self.validator.evolve(schema = schema)
            valid = self.validator.is_valid(quest)
            if valid:
                quest_type = name
                break

        # Las misiones de delivery y retrieval tienen la misma forma, solo se diferencian
        # en el personaje extra
        if quest_type == "delivery_retrieval":
            character = relationships.find_character(quest)
            quest_type = "delivery"
            if character[1] == "item_provider_name":
                quest_type = "retrieval"

        elif quest_type == "request":
            character = relationships.find_character(quest)
            if not character:
                quest_type += "_oneself"

        return quest_type