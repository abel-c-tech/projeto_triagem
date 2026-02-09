from fastapi import APIRouter
from pydantic import BaseModel
import re
from config.nlp import get_nlp

router = APIRouter(prefix="/analisar", tags=["Análise"])


class CurriculoRequest(BaseModel):
    texto: str


@router.post("/")
def analisar_curriculo(dados: CurriculoRequest):
    texto = dados.texto
    nlp = get_nlp()
    doc = nlp(texto)

    # -----------------------------
    # Nome (NER - Pessoa)
    # -----------------------------
    nomes = [
        ent.text for ent in doc.ents
        if ent.label_ == "PER"
    ]

    # -----------------------------
    # Email (Regex)
    # -----------------------------
    emails = re.findall(
        r"[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+",
        texto
    )

    # -----------------------------
    # Telefone (Regex)
    # -----------------------------
    telefones = re.findall(
        r"\(?\d{2}\)?\s?\d{4,5}-?\d{4}",
        texto
    )

    # -----------------------------
    # Hard Skills
    # -----------------------------
    hard_skills_base = [
        "python", "java", "c#", "sql",
        "docker", "aws", "linux", "git"
    ]

    hard_skills = {
        token.text.lower()
        for token in doc
        if token.text.lower() in hard_skills_base
    }

    return {
        "nomes_detectados": list(set(nomes)),
        "emails": list(set(emails)),
        "telefones": list(set(telefones)),
        "hard_skills": list(hard_skills)
    }