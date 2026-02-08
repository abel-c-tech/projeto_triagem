import spacy
from fastapi import FastAPI
from api.controller import router
import uvicorn

# -------------------------------------------------
# Inicialização do modelo NLP (carrega UMA vez)
# -------------------------------------------------
nlp = spacy.load("pt_core_news_sm")


def get_nlp():
    """
    Retorna o modelo NLP carregado.
    Usado por controllers/services.
    """
    return nlp


# -------------------------------------------------
# Criação da API
# -------------------------------------------------
app = FastAPI(
    title="Agente NLP - Classificador de Talentos",
    description="API responsável por extração e classificação de currículos",
    version="1.0.0"
)

app.include_router(router)

# -------------------------------------------------
# Endpoint básico de saúde
# -------------------------------------------------
@app.get("/")
def healthcheck():
    return {
        "status": "online",
        "service": "agente-nlp"
    }

if __name__ == "__main__":
    uvicorn.run(
        "main:app",
        host="127.0.0.1",
        port=8000,
        reload=True
    )