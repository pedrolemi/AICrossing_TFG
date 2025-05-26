from langchain_groq import ChatGroq
from langchain_core.prompts import ChatPromptTemplate
import relationships
import re

# Clase para generar un texto a partir de la descripcion de una mision
# Se usa la tecnica de Chain of Thought
class TitleGenerator():
    def __init__(self, model_name):
        self.create_start_chains(model_name)

        # Grupo de captura de cualquier letra (\w)
        pattern = r": (\w)"
        self.re_format = re.compile(pattern)

    def create_start_chains(self, model_name):        
        system = '''Eres un experto en crear títulos atractivos para misiones de un videojuego RPG. 
Tu tarea es generar un título breve, intrigante y cautivador a partir de la descripción de una misión narrada en primera persona.
El título debe despertar emociones o curiosidad, motivando al jugador a aceptar la misión. Debe ser creativo, evocador y memorable, pero sin caer en la exageración.'''

        human_base = '''Q: {description}
A: Piensa paso por paso, considerando diversas posibilidades y evaluando sus pros y sus contras.'''

        # Se trata de los mismos mensajes del sistema y humano, pero anadiendo en el segundo paso
        # el pensamiento generado en el primero
        self.first_step_chain = self.create_first_step_chain(model_name, system, human_base)
        self.second_step_chain = self.create_second_step_chain(model_name, system, human_base)

    def create_first_step_chain(self, model_name, system, human_base):
        model = ChatGroq(
            model_name = model_name,
            temperature = 0.6,
        )

        chat_prompt = ChatPromptTemplate.from_messages([
            ("system", system),
            ("human", human_base)
        ])

        return chat_prompt | model

    def create_second_step_chain(self, model_name, system, human_base):
        model = ChatGroq(
            model_name = model_name,
            temperature = 0.6,
            max_tokens = 12,
        )

        human = human_base + '''

{thinking}

Considerando todas las opciones anteriores, devuelve únicamente el título que consideres mas apropiado, sin información adicional:
'''

        chat_prompt = ChatPromptTemplate.from_messages([
            ("system", system),
            ("human", human),
        ])

        return chat_prompt | model

    def format(self, title, character):
        # Se eliminan los "...", porque normalmente los genera
        title = title.strip('\"')

        # Se pone como mayuscula solo la primera letra
        title = title.capitalize()

        # match contiene la captura
        def repl(match):
            # Se coge mayor que 0, para que corresponda con lo que se ha capturado
            aux = match.group(1)
            return f": {aux.upper()}"
        
        # Si el titulo esta formado por dos partes, separadas por ":", se pone en mayuscula
        # la primera letra de la segunda parte
        title = self.re_format.sub(repl, title)

        # Se pone en mayuscula el nombre del personaje (si existe)
        if character:
            character_name = character[0]
            character_name_lower = character_name.lower()
            title = title.replace(character_name_lower, character_name)
        return title

    def first_step(self, quest_description):
        assistant = self.first_step_chain.invoke({"description": quest_description})

        return assistant.content
    
    def second_step(self, quest_description, thinking):
        assistant = self.second_step_chain.invoke({"description": quest_description, 
                                            "thinking": thinking})

        return assistant.content

    def generate(self, quest, quest_description):
        # Se obtiene el nombre del personaje extra (si existe)
        character = relationships.find_character(quest)

        thinking = self.first_step(quest_description)
        title = self.second_step(quest_description, thinking)
        return self.format(title, character)