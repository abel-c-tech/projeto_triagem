import spacy

# Carrega o modelo UMA vez
nlp = spacy.load("pt_core_news_sm")

def get_nlp():
    return nlp