from quest_validator import QuestValidator
from quest_generator import QuestGenerator
from title_generator import TitleGenerator
from text_analyzer import TextAnalyzer
from loader import QuestLoader
import loader
import relationships
# import getpass
import os
import time

os.environ["GROQ_API_KEY"] = "..."
# if "GROQ_API_KEY" not in os.environ:
    # os.environ["GROQ_API_KEY"] = getpass.getpass("Introduce tu API Key de Groq Cloud: ")

# Se trata del programa principal
if __name__ == "__main__":
    print("Starting quest generation process...", flush=True)

    print("Initializing quest validator and text analyzer...", flush=True)
    # Comprobar el tipo de misiones
    quest_validator = QuestValidator()
    # Analaizar la legibilidad del texto
    text_analyzer = TextAnalyzer()

    print("Building character relationship graph and loading data (descriptions and items)...", flush=True)
    # Se obtienen las relaciones entre los personajes
    graph, types = relationships.create_graph()
    # relationships.show_graph(graph)
    # Se obtienen las descripciones de los personajes
    descriptions = loader.load_descriptions()
    # Se obtienen los items que hay que en el juego
    regular_items = loader.load_regular_items()
    lost_items = loader.load_lost_items()
    items = regular_items | lost_items

    # Modelo que usar
    model_name = "llama-3.3-70b-versatile"

    print(f"Initializing generators with model: {model_name}.", flush=True)
    n_examples = 3
    quest_generator = QuestGenerator(model_name, graph, types, descriptions, items, n_examples)
    title_generator = TitleGenerator(model_name)
    quest_loader = QuestLoader()

    quest_count = 0

    print("Loading input quests...", flush=True)
    # Se obtienen las misiones, ubicadas en el directorio inputs
    quests = quest_loader.load_quests()

    quests_len = len(quests)

    print("\n" + "-" * 60 + "\n", flush=True)

    for index, (name, quest) in enumerate(quests.items()):
        # Si existe una mision con el mismo nombre en el directorio output, quiere decir que ya se ha creado
        if not quest_loader.quest_exists(name, quest):
            print(f"📜 Generation quest: {name}.", flush=True)
            # Se identifica el tipo de mision
            quest_type = quest_validator.find_quest_type(quest)

            if quest_type:
                print(f"✅ Quest type detected: {quest_type}", flush=True)
                # Se genera la descripcion de la mision
                print(f"🛠️ Generating quest description...", flush=True)
                quest_description = quest_generator.generate(quest_type, quest)
                print(f"✅ Quest description generated succesfully.", flush=True)
                
                # Se genera el titulo de la mision
                print(f"🏷️ Generating quest title...")
                quest_title = title_generator.generate(quest, quest_description)
                print(f"✅ Quest title generated succesfully.", flush=True)

                # Se analiza la legibilidad del texto
                print(f"📊 Analyzing legibility of the quest description.")
                readability = text_analyzer.analyze(quest_description)
                print("✅ Readability analysis completed.", flush=True)
                
                # Se agrega la mision, para su posterior escritura
                quest_loader.add_quest(name, quest, quest_description, quest_title, readability)

                quest_count += 1

                if index < quests_len - 1:
                    print("⏳ Waiting 1 minute before processing next quest to avoid server limits...", flush=True)
                    time.sleep(60)
                    print("⏱️ Resuming quest generation.")
            else:
                print("❌ Quest is no properly defined and will be skipped.", flush=True)
        else:
            print(f"⚠️ Quest '{name}' already exists in output. Skipping...", flush=True)
        
        print("\n" + "-" * 60 + "\n", flush=True)

    # Se escriben las misiones en el directorio output
    print("📁 Writing all generated quests to output directory...", flush=True)
    quest_loader.write_quests()
    print(f"✅ Quest generation complete. Total quests created: {quest_count}.", flush=True)