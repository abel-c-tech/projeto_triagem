from fastapi import APIRouter
from pydantic import BaseModel
import re
from config.nlp import get_nlp
from services.classifier import classificar_perfil

router = APIRouter(prefix="/analisar", tags=["Análise"])


class CurriculoRequest(BaseModel):
    texto: str


@router.post("/")
def analisar_curriculo(dados: CurriculoRequest):
    texto = dados.texto
    nlp = get_nlp()
    doc = nlp(texto)

    # -----------------------------
    # Classificação RF04
    # -----------------------------
    perfil, confianca = classificar_perfil(doc, nlp)

    # -----------------------------
    # Nome (NER + fallback)
    # -----------------------------
    nomes = [
        ent.text for ent in doc.ents
        if ent.label_ == "PER"
    ]

    if not nomes:
        match = re.search(r"meu nome é ([A-ZÁÉÍÓÚÂÊÔÃÕ][a-záéíóúâêôãõ]+)", texto)
        if match:
            nomes.append(match.group(1))

    # -----------------------------
    # Email
    # -----------------------------
    emails = re.findall(
        r"[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+",
        texto
    )

    # -----------------------------
    # Telefone
    # -----------------------------
    telefones = re.findall(
        r"\(?\d{2}\)?\s?\d{4,5}-?\d{4}",
        texto
    )

    # -----------------------------
    # Hard Skills (similaridade semântica)
    # -----------------------------
    conceitos_base = [
        "python", "java", "javascript", "html", "css",
        "docker", "banco de dados", "computação em nuvem",
        "engenharia", "análise de dados", "linguagem de programação",
        "desenvolvimento backend", "api", "servidor",  "desenvolvimento frontend",
        "interface do usuário","web", "c#", "c", "javascript","machine learning",
        "inteligência artificial", "automação","design", "modelagem",
        "software", "sql", "nosql", "linux", "windows", "git", "ci/cd"
    ]

    conceitos_docs = [nlp(c) for c in conceitos_base]
    hard_skills = set()

    for token in doc:
        if token.is_stop or token.is_punct or not token.has_vector:
            continue

        for conceito_doc in conceitos_docs:
            if token.similarity(conceito_doc) >= 0.60:
                hard_skills.add(token.text.lower())
                break

    return {
        "nomes_detectados": list(set(nomes)),
        "emails": list(set(emails)),
        "telefones": list(set(telefones)),
        "hard_skills": list(hard_skills),
        "perfil": perfil,
        "confianca": confianca
    }