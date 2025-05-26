from abc import ABC, abstractmethod
from syllables_counter import SyllablesCounter
import spacy

# Se trata de una clase abstradta
class ReadabilityFormula(ABC):
    def __init__(self):
        self.syllables_counter = SyllablesCounter()

    def get_sentences(self, doc):
        return list(doc.sents)
    
    def get_number_sentences(self, doc):
        sentences = self.get_sentences(doc)
        return len(sentences)
    
    def get_words(self, doc):
        words = []
        for token in doc:
            # Se cuentan las palabra que hay en el texto (que no son simbolos ni espacios)
            if not token.is_punct and not token.is_space:
                words.append(token.text)
        return words
    
    def get_number_words(self, doc):
        words = self.get_words(doc)
        return len(words)
    
    def get_number_letters(self, doc):
        words = self.get_words(doc)
        return sum(len(word) for word in words)
    
    def get_syllables_per_word(self, doc):
        words = self.get_words(doc)
        syllables = [(word, self.syllables_counter(word)) for word in words]
        return syllables
    
    def get_number_syllables(self, doc):
        syllables_per_word = self.get_syllables_per_word(doc)
        return sum(syllables for word, syllables in syllables_per_word)
    
    @abstractmethod
    def get_id(self):
        pass

    @abstractmethod
    def calculate(self, doc):
        pass

    @abstractmethod
    def translate(self, score):
        pass

    def calculate_translate(self, doc):
        score = self.calculate(doc)
        return self.translate(score)

class FleschReadingEaseScore(ReadabilityFormula):
    def get_id(self):
        return "flesch_reading_ease_score"
    
    def calculate(self, doc):
        number_words = self.get_number_words(doc)
        number_sentences = self.get_number_sentences(doc)
        number_syllables = self.get_number_syllables(doc)

        words_per_sentence = number_words / number_sentences
        syllables_per_word = number_syllables / number_words

        return 206.835 - (1.015 * words_per_sentence) - (84.6 * syllables_per_word)
    
    def translate(self, score):
        if score >= 90:
            return "muy fácil"
        elif score >= 80:
            return "fácil"
        elif score >= 70:
            return "bastante fácil"
        elif score >= 60:
            return "estándar"
        elif score >= 50:
            return "bastante difícil"
        elif score >= 30:
            return "difícil"
        else:
            return "muy confusa"
        
class FleschKincaidGradeLevel(ReadabilityFormula):
    def get_id(self):
        return "flesch_kincaid_grade_level"
    
    def calculate(self, doc):
        number_words = self.get_number_words(doc)
        number_sentences = self.get_number_sentences(doc)
        number_syllables = self.get_number_syllables(doc)

        words_per_sentence = number_words / number_sentences
        syllables_per_word = number_syllables / number_words

        return 0.39 * words_per_sentence + 11.8 * syllables_per_word - 15.59
    
    def translate(self, score):
        if score >= 12:
            return "avanzado"
        elif score >= 6:
            return "medio"
        else:
            return "básico"

class AutomatedReadabilityIndex(ReadabilityFormula):
    def get_id(self):
        return "automated_readability_index"
    
    def calculate(self, doc):
        number_letters = self.get_number_letters(doc)
        number_words = self.get_number_words(doc)
        number_sentences = self.get_number_sentences(doc)
        
        letters_per_word = number_letters / number_words
        words_per_sentence = number_words / number_sentences
        
        return 4.71 * letters_per_word + 0.5 * words_per_sentence - 21.43
    
    def translate(self, score):
        if score >= 13:
            return "muy difícil"
        elif score >= 12:
            return "difícil"
        elif score >= 11:
            return "bastante difícil"
        elif score >= 10:
            return "algo difícil"
        elif score >= 9:
            return "un poco difícil"
        elif score >= 7:
            return "medio"
        elif score >= 5:
            return "fácil-medio"
        elif score >= 4:
            return "fácil"
        elif score >= 2:
            return "muy fácil"
        else:
            return "extremadamente fácil"
        
class GunningFogIndex(ReadabilityFormula):
    def get_id(self):
        return "gunning_fog_index"

    # Las palabras complejas son palabras que no son comunes en el idioma (is_stop) y que tienen 3 o mas silabas
    def get_complex_words(self, doc):
        complex_words = []
        for token in doc:
            if not token.is_punct and not token.is_space and not token.is_stop:
                number_syllables = self.syllables_counter(token.text)
                if number_syllables > 2:
                    complex_words.append(token.text)
        return complex_words
    
    def get_number_complex_words(self, doc):
        complex_words = self.get_complex_words(doc)
        return len(complex_words)
    
    def calculate(self, doc):
        number_words = self.get_number_words(doc)
        number_sentences = self.get_number_sentences(doc)

        words_per_sentence = number_words / number_sentences
        complex_words_per = self.get_number_complex_words(doc) / number_words

        return 0.4 * (words_per_sentence + 100 * complex_words_per)
    
    def translate(self, score):
        if score >= 17:
            return "muy difícil"
        elif score >= 14:
            return "difícil"
        elif score >= 13:
            return "algo difícil"
        elif score >= 11:
            return "un poco difícil"
        elif score >= 8:
            return "medio"
        else:
            return "fácil"
        
class ColemanLiauIndex(ReadabilityFormula):
    def __init__(self):
        super().__init__()
        self.HUNDRED_WORDS = 100

    def get_id(self):
        return "coleman_liau_index"

    def calculate(self, doc):
        number_letters = self.get_number_letters(doc)
        number_words = self.get_number_words(doc)
        number_sentences = self.get_number_sentences(doc)

        letters_per_word = number_letters / number_words
        sentences_per_word =  number_sentences / number_words

        letters_per_hundred_words = letters_per_word * self.HUNDRED_WORDS
        sentences_per_hundred_words = sentences_per_word * self.HUNDRED_WORDS
        return 0.0588 * letters_per_hundred_words - 0.296 * sentences_per_hundred_words - 15.8
    
    def translate(self, score):
        if score >= 17:
            return "muy difícil"
        elif score >= 14:
            return "difícil"
        elif score >= 13:
            return "algo difícil"
        elif score >= 11:
            return "un poco difícil"
        elif score >= 8:
            return "medio"
        else:
            return "fácil"

# Clase para analizar la legibilidad de un texto usando diversas formulas
class TextAnalyzer:
    def __init__(self):
        # Se utiliza un modelo de spacy para obtener infromacion acerca del texto
        self.nlp = spacy.load("es_core_news_sm")

        # Formulas que se usan
        self.formulas = [FleschReadingEaseScore(), FleschKincaidGradeLevel(), AutomatedReadabilityIndex(), GunningFogIndex(), ColemanLiauIndex()]
    
    def analyze(self, text):
        doc = self.nlp(text)

        scores = {}
        for formula in self.formulas:
            scores[formula.get_id()] = formula.calculate_translate(doc)

        return scores