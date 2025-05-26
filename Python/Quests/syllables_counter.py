import re

# Se utiliza para contar el numero de silabas de una palabra, que se necesita para analizar la legibilidad de un texto
class SyllablesCounter:
    # Diferentes tipos de vocales
    STRONG_UNSTRESSED_VOWELS = "aeo"
    STRONG_STRESSED_VOWELS = "áéó"
    STRONG_VOWELS = STRONG_UNSTRESSED_VOWELS + STRONG_STRESSED_VOWELS

    WEAK_UNSTRESSED_VOWELS = "iu"
    WEAK_STRESSED_VOWELS = "íú"
    WEAK_VOWELS = WEAK_UNSTRESSED_VOWELS + WEAK_STRESSED_VOWELS

    UNSTRESSED_VOWELS = STRONG_UNSTRESSED_VOWELS + WEAK_UNSTRESSED_VOWELS
    STRESSED_VOWELS = STRONG_STRESSED_VOWELS + WEAK_STRESSED_VOWELS

    VOWELS = UNSTRESSED_VOWELS + STRESSED_VOWELS

    def __init__(self):    
        # Se produce un triptongo si VOCAL CERRADA ATONA + VOCAL ABIERTA + VOCAL CERRADA ATONA
        tripthong_pattern = f"[{self.WEAK_UNSTRESSED_VOWELS}][{self.STRONG_VOWELS}][{self.WEAK_UNSTRESSED_VOWELS}]"
        self.re_tripthongs = re.compile(tripthong_pattern)

        # Se produce un diptongo si:
        # - VOCAL ABIERTA + VOCAL CERRADA ATONA
        # - VOCAL CERRADA ATONA + VOCAL ABIERTA
        # - VOCAL CERRADA ATONA + VOCAL CERRADA
        diphthong_pattern = f"[{self.STRONG_VOWELS}][{self.WEAK_UNSTRESSED_VOWELS}]|[{self.WEAK_UNSTRESSED_VOWELS}][{self.STRONG_VOWELS}]|[{self.WEAK_UNSTRESSED_VOWELS}][{self.WEAK_VOWELS}]"
        self.re_diphthongs = re.compile(diphthong_pattern)

    def count_vowels(self, word):
        vowels_set = set(self.VOWELS)

        number_vowels = 0
        for letter in word:
            if letter in vowels_set:
                number_vowels = number_vowels + 1
        return number_vowels
    
    # Se usan expresiones regulares para detectar el numero de diptongos
    def count_tripthongs(self, word):
        tripthongs = self.re_tripthongs.findall(word)
        return len(tripthongs)

    def count_diphthongs(self, word):
        diphthongs = self.re_diphthongs.findall(word)
        return len(diphthongs)

    def count_syllables(self, word):
        # Se convierte toda la palabra a minuscula
        word_aux = word.lower()
        
        # En cuanto a la fonetica, en espanol, cuando la "y" griega aparece al final de palabra,
        # se pronuncia como una  "i" latina. Por lo tanto, a efectos de contar los diptongos y triptongos, 
        # se considera como una vocal cerrada mas
        if word_aux[-1] == 'y':
            word_aux = word_aux[:-1] + 'i'

        # En espanol en todas las silabas hay como minimo una vocal, por lo tanto, 
        # es una buena aproximacion contar el numero de vocales para saber cuantas silabas tiene una palabra
        number_vowels = self.count_vowels(word_aux)
        if number_vowels <= 0:
            return 1
        
        # Los diptongos son dos vocales juntas que estan dentro de la misma silaba.
        # En cambio, los hiatos estan en diferentes silabas.

        # Por lo tanto, sabiendo hay que cada silaba esta formada por minomo una vocal,
        # hay que encontrar el numero de triptongos y diptongos de una palabra
        # para restarselo y asi, determinar el numero de silabas

        # Se obtiene el numero de triptongos
        number_tripthongs = self.count_tripthongs(word_aux)
        # Se multiplica por 2 porque un triptongo esta formado por 3 vocales
        number_vowels = number_vowels - number_tripthongs * 2

        # No existen palabras con triptongos que tengan diptongos
        if number_tripthongs <= 0:
            # Se obtiene el numero diptongos
            number_diphthongs = self.count_diphthongs(word_aux)
            number_vowels = number_vowels - number_diphthongs

        return number_vowels

    def __call__(self, word):
        return self.count_syllables(word)

# Comprobar el accurazcy de la clase con un dataset obtenido de
# https://link.springer.com/chapter/10.1007/978-3-540-24630-5_49
def test():
    test_words = [
        ("cierto", 2),
        ("hombre", 2),
        ("había", 3),
        ("comprado", 3),
        ("una", 2),
        ("vaca", 2),
        ("magnífica", 4),
        ("soñó", 2),
        ("misma", 2),
        ("noche", 2),
        ("crecían", 3),
        ("sobre", 2),
        ("espaldas", 3),
        ("animal", 3),
        ("marchaba", 3),
        ("volando", 3),
        ("considerando", 5),
        ("esto", 2),
        ("presagio", 3),
        ("infortunio", 4),
        ("inminente", 4),
        ("llevó", 2),
        ("mercado", 3),
        ("nuevamente", 4),
        ("vendió", 2),
        ("gran", 1),
        ("pérdida", 3),
        ("envolviendo", 4),
        ("pano", 2),
        ("plata", 2),
        ("recibió", 3),
        ("echó", 2),
        ("mitad", 2),
        ("camino", 3),
        ("casa", 2),
        ("halcón", 2),
        ("comiendo", 3),
        ("parte", 2),
        ("libre", 2),
        ("acercándose", 5),
        ("ave", 2),
        ("descubrió", 3),
        ("bastante", 3),
        ("mansa", 2),
        ("manera", 3),
        ("ató", 2),
        ("pata", 2),
        ("esquina", 3),
        ("pano", 2),
        ("dinero", 3),
        ("aleteaba", 5),
        ("mucho", 2),
        ("tratando", 3),
        ("escapar", 3),
        ("rato", 2),
        ("aflojarse", 4),
        ("momentáneamente", 7),
        ("voló", 2),
        ("trapo", 2),
        ("destino", 3),
        ("dijo", 2),
        ("hombre", 2),
        ("cuando", 2),
        ("historia", 3),
        ("ignorante", 4),
        ("tenerse", 3),
        ("fe", 1),
        ("sueños", 2),
        ("segundo", 3),
        ("gente", 2),
        ("debe", 2),
        ("recoger", 3),
        ("cosas", 2),
        ("lado", 2),
        ("camino", 3),
        ("cuadrúpedos", 4)
    ]

    syllables_counter = SyllablesCounter()

    successes = 0
    for word, n in test_words:
        number_syllables = syllables_counter(word)
        if number_syllables == n:
            successes += 1

    accuracy = successes / len(test_words)
    print(f"The algorithm of SyllablesCounter has an accuracy of {accuracy} against test words.")
