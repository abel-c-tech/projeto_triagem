from fastapi import APIRouter
from pydantic import BaseModel

# -------------------------------------------------
# Criação do router
# -------------------------------------------------
router = APIRouter(prefix="/analisar", tags=["Análise"])


# -------------------------------------------------
# Modelo de entrada (request body)
# -------------------------------------------------
class CurriculoRequest(BaseModel):
    texto: str


# -------------------------------------------------
# Endpoint POST /analisar
# -------------------------------------------------
@router.post("/")
def analisar_curriculo(dados: CurriculoRequest):
    """
    Recebe o texto de um currículo e retorna
    uma resposta simples (mock).
    """
    return {
        "mensagem": "Currículo recebido com sucesso",
        "tamanho_texto": len(dados.texto)
    }