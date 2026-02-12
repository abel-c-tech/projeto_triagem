import spacy

# Carrega o modelo UMA vez
nlp = spacy.load("pt_core_news_lg")

def get_nlp():
    return nlp