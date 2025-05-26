from langchain_groq import ChatGroq
from langchain_core.prompts import ChatPromptTemplate, FewShotChatMessagePromptTemplate
from langchain_core.messages import SystemMessage
import json
import copy
import random
import loader
import relationships

# Clase que utiliza la tecnica de SG-ICL para crear la descripcion de una mision
class QuestGenerator:
    def __init__(self, model_name, graph, types, descriptions, items, n_examples = 2):
        self.model_name = model_name

        # Informacion del juego
        self.graph = graph
        self.types = types
        self.descriptions = descriptions
        self.items = items

        # Numero de ejemplos que crear
        self.n_examples = n_examples

        # Se obtienen los esquemas de los resumenes las misiones
        self.quests_summaries_schemas = loader.load_quests_summaries_schemas()
        # Se obtienen los resumenes de las misiones
        self.quests_summaries = loader.load_quests_summaries()
        # Se obtienen las descripciones de las misiones
        self.quests_descriptions = loader.load_quests_descriptions()

        # Se obtiene el dataset de nombres
        self.first_names_dataset = loader.load_first_names()

        # Contexto del juego
        self.GAME_CONTEXT = "El videojuego se desarrolla en un pequeño pueblo donde el jugador, tras mudarse, debe ayudar a los personajes con diversas tareas y construir relaciones de amistad con ellos."

        # Se crean las cadenas, que se utilzaran posteriormente
        self.create_start_chains(model_name)

    def create_start_chains(self, model_name):
        self.second_third_steps_model = ChatGroq(
            model_name = model_name,
            temperature = 0.6,
            max_tokens = 100,
        )

        # Se obtienen las misiones de stardew valley, que se usan de referencia
        stardew_valley_quests = loader.load_stardew_valley_quests()
        stardew_valley_quests = '\n'.join(f"    - {quest}" for quest in stardew_valley_quests)
        
        self.second_third_steps_system = f'''Eres un asistente especializado en redactar una misión de un videojuego RPG, a partir de la descripción de la misma.
La misión debe ser redactada de la siguiente manera:
- El OBJETIVO debe quedar claro.
- La misión debe ser breve, con un máximo de 2 líneas.
- Estará escrita en primera persona, desde el punto de vista del personaje que entrega la misión.
- Evita mencionar el nombre del personaje que entrega la misión.
- Los números deben expresarse como cifras (por ejemplo, 5 en vez de "cinco").
- Si separar el texto en oraciones mejora la comprensión, hazlo.
- Si es necesario, inventa detalles para enriquecer la misión.
- El tono debe ser informal y desenfadado, como el de las siguientes misiones:
{stardew_valley_quests}'''
        
        self.second_third_steps_human = f'''Descripción de la misión:
{self.GAME_CONTEXT}''' + '''
{description}

Misión:
'''
        # Se crea la cadena del segundo paso
        self.second_step_chain = self.create_second_step_chain(self.second_third_steps_model, self.second_third_steps_system, self.second_third_steps_human)

    # Se cree la cadena del primer paso
    def create_first_step_chain(self, model_name, quest_type):
        json_schema = self.quests_summaries_schemas[quest_type]
        json_formatted_str = json.dumps(json_schema, indent=4, ensure_ascii=False)

        quest_summary = self.quests_summaries[quest_type]

        # El objetivo es obtener los campos necesarios para crear una mision de ejemplo
        system = f'''Eres un asistente especializado en completar la información de la descripción de una misión de un videojuego RGP.
        
CONTEXTO DEL VIDEOJUEGO:
{self.GAME_CONTEXT}

DESCRIPCION DE LA MISION:
{quest_summary}

INSTRUCCIONES CLAVES:
Cuando el usuario solicite la creación de un conjunto de misiones, tu tarea es completar EXCLUSIVAMENTE los campos entre corchetes [...], asegurándote de que:
- La información agregada sea coherente con la ambientanción del videojuego.
- Los campos agregados no sean demasiado extensos.
- Todo el contenido esté bien contextualizado dentro de la descripción.
- No se incluyan nombres propios para los personajes, solo descripciones que indiquen su personalidad o rol en la comunidad.
- Todos los campos deben escribirse en minúscula.
- No agregues determinantes al objeto.

FORMATO DE RESPUESTA:
La respuesta debe estructurarse en formato JSON, donde cada misión se representa como un objeto dentro de un array bajo la clave "quests".
Cada misión se compone de varios campos, los cuales corresponden directamente a los campos entre corchetes [...] en la descripción de la misión.
El JSON debe cumplir con el siguiente JSON Schema:
{json_formatted_str}'''
        
        system_message = SystemMessage(content = system)
        human = "Genera {n_examples} misiones con descripciones diversas y creativas. Asegúrate de que cada misión tenga personajes con personalidades distintas, objetos únicos y relaciones variadas entre ellos. También, procura que los temas de las misiones sean diferentes para aportar más variedad al juego."

        chat_prompt = ChatPromptTemplate.from_messages([
            system_message,
            ("human", human)
        ])

        # chat_prompt.partial(n_examples = n_examples)
        model = ChatGroq(
            model_name = model_name,
            temperature = 0.6
        )

        structured_model = model.with_structured_output(json_schema, method='json_mode')

        return chat_prompt | structured_model

    # Se crea la cadena del segundo paso, que se encarga de crear la descripcion para las misiones de ejemplo
    def create_second_step_chain(self, model, system, human):        
        chat_prompt = ChatPromptTemplate.from_messages([
            ("system", system),
            ("human", human)
        ])

        return chat_prompt | model
    
    # Se crea la cadena del tercer paso, que es muy similar a la anterior, pero se utiliza para crear
    # la descripcion de la misiones definitiva usando como ejemplos las misiones que se han creado anteriormente al vuelo
    def create_third_step_chain(self, model, examples, system, human):
        example_prompt = ChatPromptTemplate.from_messages([
            ("human", "{input}"),
            ("ai", "{output}"),
        ])

        few_shot_prompt = FewShotChatMessagePromptTemplate(
            example_prompt = example_prompt,
            examples = examples,
        )

        final_prompt = ChatPromptTemplate.from_messages([
            ("system", system),
            few_shot_prompt,
            ("human", human),
        ])

        return final_prompt | model

    # Se ejecuta el primera paso
    def first_step(self, quest_type):
        chain = self.create_first_step_chain(self.model_name, quest_type)
        json_object = chain.invoke({"n_examples": self.n_examples})
        return json_object
    
    # Se utiliza para rellenar los campos de la plantilla de una mision a partir de un json
    # Se usa en los pasos 2 y 3
    def replace_params(self, quest_description, params, prefix = ""):
        for key, value in params.items():
            if isinstance(value, dict):
                prefix_aux = prefix + f"{key}_"
                quest_description = self.replace_params(quest_description, value, prefix_aux)
            else:
                aux = f"[{prefix + key}]"
                quest_description = quest_description.replace(aux, str(value))
        return quest_description

    # Se ejecuta el paso numero 2, que rellena la plantilla de una mision de ejemplo, a partir de los campos obtenidos en el paso anterior
    def second_step(self, quest_type, example_quests):
        # Los jsons obtenidos en el paso anterior, con los campos de las misiones de ejemplo
        # no contienen todos los datos, sino que algunos se obtienen de otra forma

        # Los nombres se obtienen de un dataset
        random_names = random.sample(self.first_names_dataset, self.n_examples * 2)

        # El numero de items a obtener se genera de forma aleatoria
        MIN_ITEMS = 1
        MAX_ITEMS = 5

        examples = []
        for i in range(self.n_examples):
            quest_description = self.quests_descriptions[quest_type]

            quest = example_quests[i]
            # Se obtiene el nombre de los personajes, uno el que da la mision y el otro, el personaje extra
            character1 = random_names[2 * i]
            character2 = random_names[(2 * i) + 1]

            # Se reemplaza el numero de items
            rand_int = random.randint(MIN_ITEMS, MAX_ITEMS)
            quest_description = quest_description.replace("[item_amount]", str(rand_int))
            # Se reemplazan los nombres
            quest_description = quest_description.replace("[quest_giver_name]", character1)
            quest_description = quest_description.replace("[character_name]", character2)

            # Se reemplazan los campos obtenidos en el paso anterior
            quest_description = self.replace_params(quest_description, quest)
            
            assistant = self.second_step_chain.invoke({"description": quest_description})

            examples.append({
                "input": quest_description,
                "output": assistant.content
            })
        
        return examples

    def fill_params(self, quest):
        quest = copy.deepcopy(quest)

        quest_giver_name = quest["quest_giver_name"]
        # Si la mison tiene un personaje extra
        character = relationships.find_character(quest)
        if character:
            character_name = character[0]
            # Se reemplaza el nombre y su descripcion
            quest["character_name"] = character_name
            quest["character_description"] = self.descriptions[character_name]
            # Se reemplaza la relacion entre el persoanje que da la mision y el otro
            value = self.graph.get_edge_data(quest_giver_name, character_name)["weight"]
            type = self.types[value]
            quest["relationship"] = type

        # Se reemplaza la descripcion
        quest["quest_giver_description"] = self.descriptions[quest_giver_name]

        item = quest["item"]
        item_id = item["id"]
        item_names = self.items[item_id]
        item_amount = item["amount"]
        if item_amount <= 1:
            quest["item"]["id"] = item_names[0]
        else:
            quest["item"]["id"] = item_names[1]

        return quest

    # Se ejecuta el tercer paso, que genera la descripcion para la mision usando los ejemplos generados al vuelo
    def third_step(self, examples, quest_type, quest):
        chain = self.create_third_step_chain(self.second_third_steps_model, examples, self.second_third_steps_system, self.second_third_steps_human)

        quest_description = self.quests_descriptions[quest_type]

        # Se agregan los parametros necesarios a la mision
        quest = self.fill_params(quest)

        # Se reemplazan el resto de parametros, que vienen indicados en el json
        quest_description = self.replace_params(quest_description, quest)

        assistant = chain.invoke({"description": quest_description})
        
        return assistant.content.strip()

    def generate(self, quest_type, quest):
        example_quests = self.first_step(quest_type)
        quests = example_quests.get("quests")
        if not quest:
            quests = example_quests["properties"]["quests"]
        examples = self.second_step(quest_type, quests)
        return self.third_step(examples, quest_type, quest)