import networkx as nx
import matplotlib.pyplot as plt
import loader

# Mostrar el grafo de relaciones
def show_graph(graph):
    COLORS = {
        0: '#B0B0B0',
        1: '#FF6666', 
        2: '#FFA500',
        3: '#87CEFA',
        4: '#32CD32',
    }
    LABELS = {
        0: 'Neutral',
        1: 'Tense',
        2: 'Distant',
        3: 'Friendly',
        4: 'Close',
    }

    edge_labels = nx.get_edge_attributes(graph, 'weight')
    weights = list(edge_labels.values())

    edge_labels = {key: LABELS[value] for key, value in edge_labels.items()}
    edge_colors = [COLORS[weight] for weight in weights]

    plt.figure(figsize=(10, 8))
    pos = nx.circular_layout(graph)

    nx.draw_networkx_nodes(graph, pos, node_color = 'lightskyblue', node_size = 5000, edgecolors = 'black', margins=0.2)
    nx.draw_networkx_edges(graph, pos, width = 4.0, edge_color = edge_colors)
    nx.draw_networkx_labels(graph, pos)
    nx.draw_networkx_edge_labels(graph, pos, edge_labels=edge_labels, font_size=15)

    plt.title("Relationships between characters")
    plt.axis('off')

    plt.tight_layout()
    plt.show()

# Crear el grafo de relaciones
def create_graph():
    graph = nx.Graph()

    weights, types = loader.load_relationships()

    for weight, characters in weights.items():
        graph.add_edges_from(characters, weight = weight)
    
    return graph, types

# Encontrar el personaje extra (si existe)
def find_character(quest):
    quest_giver_name = quest["quest_giver_name"]

    receiver_name = quest.get("item_receiver_name")
    provider_name = quest.get("item_provider_name")

    if receiver_name and receiver_name != quest_giver_name:
        return (receiver_name, "item_reciever_name")
    elif provider_name and provider_name != quest_giver_name:
        return (provider_name, "item_provider_name")
    
    return None
