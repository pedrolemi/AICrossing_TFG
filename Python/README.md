# 🛠️ Herramienta para generar descripciones y títulos de misiones

Esta herramienta, desarrollada en Python, permite generar títulos y descripciones para distintos tipos de misiones a partir de plantillas personalizadas. 

Una vez generadas, el usuario puede modificar la narración según sus necesidades.

## 🎮 Tipos de misiones

Existen cinco tipos de misiones, cada una con su propia plantilla. Puedes encontrar ejemplos en la carpeta `\Quest\input_template`.

### Parámetros Generales

Todos los tipos de misiones utilizan los siguientes parámetros configurables:
- **Nombres de personajes**:
    - 👩 Alejandra
    - 👨 Esteban
    - 🧓 Memo
- **Objetos disponibles**: `cada objeto tiene un ID:
    - 🥕 Zanahoria (2)
    - 🥚 Huevo (4)
    - 🥬 Col (5)
    - 🥛 Leche (6)
    - 🎃 Calabaza (9)
    - 🪨 Roca (11)
    - 🌾 Trigo (13)
    - 🪵 Madera (14)
      
  Se pueden solictiar entre 1 y 5 objetos.
  
- **Puntos de amistad**: valor entre 1 y 3.
- **Tema**: palabras claves que describen la misión. Ejemplo: "alimentar animales".

## 🧩 Plantillas por tipo de misión

### 🚚 Delivery
*quest_giver_name* da uno o varios objetos al jugador, que debe llevalors a *item_receiver_name*.

```json
{
    "quest_giver_name": "Nombre1",
    "item": {
        "id": 2,
        "amount": 5
    },
    "reward": {
        "friendship_points": 3
    },
    "item_provider_name": "Nombre1",
    "item_receiver_name": "Nombre2",
    "topic": "..."
}
```

### 🌿 Gather
*quest_giver_name* pide al jugador recolectar uno o varios objetos, sin necesidad entregarlos.

```json
{
    "quest_giver_name": "Nombre1",
    "item": {
        "id": 2,
        "amount": 5
    },
    "reward": {
        "friendship_points": 3
    },
    "topic": "..."
}
```

### 🔍 Lost Item
*quest_giver_name* pide al jugador encontrar uno o varios objetos perdidos en el mundo.
- **Objetos especiales**:
    - 🪓 Hacha (15)
    - 🔨 Martillo (16)
    - ⛏️ Pico (17)
    - 🎣 Caña (18)
    - 🪤 Pala (19)
    - 🗡️ Espada (20)
    - 💧 Regadera (21)
- **Ubicaciones posibles**:
    - "corral"
    - "pozo", "piedra"
    - "acantilado"
    - "huerto"
    - "puente"
    - "cascada"
    - "casa morada"
    - "casa verde"
    - "casa roja"
    - "casa azul"
    - "plaza oeste"
    - "plaza este"

```json
{
    "quest_giver_name": "Nombre1",
    "item": {
        "id": 15,
        "amount": 5
    },
    "reward": {
        "friendship_points": 3
    },
    "location_name": "corral",
    "topic": "..."
}
```

### 📨 Request
*quest_giver_name* pide al jugador conseguir uno o varios objetos y entregarlos a sí mismo o a otra persona.

```json
{
    "quest_giver_name": "Nombre1",
    "item": {
        "id": 2,
        "amount": 5
    },
    "reward": {
        "friendship_points": 3
    },
    "item_receiver_name": "Nombre1/Nombre2",
    "topic": "..."
}
```
### 🔄 Retrieval
*quest_giver_name* pide al jugador que hable con *item_provider_name* para que le de uno o varios objetos, que debe devolverle.

```json
{
    "quest_giver_name": "Nombre2",
    "item": {
        "id": 2,
        "amount": 5
    },
    "reward": {
        "friendship_points": 3
    },
    "item_provider_name": "Nombre1",
    "item_receiver_name": "Nombre2",
    "topic": "..."
}
```

## 📊 Análisis de la legibilidad

Las misiones generadas incluirán un apartado `readiblity` que evalúa qué tan comprensible es la descripción utilizando diferentes fórmulas.

Por ejemplo, el valor de `flesh_reading_ease_score` indica que la legibilidad de la descripción creada como **muy confusa** para el lector.

```json
"readability": {
    "flesch_reading_ease_score": "muy confusa",
    "flesch_kincaid_grade_level": "avanzado",
    "automated_readability_index": "medio",
    "gunning_fog_index": "difícil",
    "coleman_liau_index": "medio"
}
```

Estas puntuaciones permiten al usuario identificar si la descripción es clara o necesita ajustes para mejorar su comprensión.

## 🛠️ Guía de uso

> ⚠️ SpaCy no es complatible con **Python 3.13** o versiones superiores. Usa **Python 3.12 o anterior** para evitar errores de compatibilidad.

### 1. Crear un entorno virtual

#### Python 3.4 o superior: 
```bash
python -m venv vEnvironmentName
```
#### Resto de versiones: 
1. Instalar virtualenv con pip install
    ```bash
    pip install virtualenv
    ```
3. Crear el entorno virtual
    ```bash
    virtualenv vEnvironmentName
    ```

### 2. Activar el entorno virtual (Windows)
- En la consola de comandos:
    ```bash
    \vEnvironmentName\Scripts\activate.bat
    ```
- En el PowerShell: 
    ```bash
    \vEnvironmentName\Scripts\Activate.ps1
    ```

### 3. Instalar requirements
Navegar a la carpeta ```\Quests``` y ejecuta:
```
pip install -r requirements.txt
```

### 4. Generar las misiones
1. Configura la API key de Groq Cloud y el modelo en `main.py`:
    ```
    os.environ["GROQ_API_KEY"] = "..."
    ...
    model_name = "..."
    ```

2. Coloca las plantillas de las misiones base en ``\Quest\input``. Estas deben seguir los esquemas JSON definidos en ``\data\quests\schemas\templates``.

4. Ejecuta ``game_python.bat`` para generar las misiones y copiarlas al directorio del juego:
    ```
    game_python.bat
    ```
    
    Para generar únicamentes las misiones, se puede ejecutar  ``main.py``:
    ```
    python main.py
    ```

5. Las misiones generadas:

    - Se guardarán en el directorio ``\Quests\output\QuestGiverName``, por si se el usuario desea realizar modificaciones.
   
    - Se copiarán al siguiente directorio para que el juego las use:
      ``%userprofile%\AppData\LocalLow\MattCastellanosPedroLeon\AICrossing\Data\NPC``

## 📁 Estructura del proyecto
```text
Quests/
├── input/                     # Misiones definidas por el usuario
├── input_template/            # Plantillas de ejemplo
├── output/                    # Misiones generada
│   └── QuestGiverName/
├── data/
│   └── quests/
│       └── schemas/
│           └── templates/     # JSON Schemas de validación
├── main.py                    # Script principal
├── game_python.bat            # Script de ejecución
├── requirements.txt           # Dependencias
```
