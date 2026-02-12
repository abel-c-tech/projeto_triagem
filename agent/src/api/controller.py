from fastapi import APIRouter
from pydantic import BaseModel
import re
import spacy
from spacy.matcher import PhraseMatcher
from typing import List, Dict

# 🔹 Router do FastAPI
router = APIRouter()

# 🔹 Setup Spacy
nlp = spacy.load("pt_core_news_lg")

# 🔹 Lista de hard skills por categoria
CATEGORIAS = {
    "Frontend": ["javascript", "react", "angular", "vue", "html", "css", "typescript", "sass", "next.js"],
    "Backend": ["node", "dotnet", "c#", "java", "python", "ruby", "php", "go", "spring", "express"],
    "Fullstack": ["javascript", "react", "angular", "html", "css", "node", "dotnet", "c#", "java", "python"],
    "Data Science": ["python", "r", "machine learning", "data analysis", "pandas", "numpy", "tensorflow", "scikit-learn", "keras"],
    "DevOps": ["docker", "kubernetes", "ci/cd", "jenkins", "ansible", "terraform", "aws", "azure", "gcp"]
}

# 🔹 Matcher para hard skills
matcher = PhraseMatcher(nlp.vocab, attr="LOWER")
all_skills = [skill for skills in CATEGORIAS.values() for skill in skills]
patterns = [nlp(skill) for skill in all_skills]
matcher.add("HARD_SKILLS", patterns)

# 🔹 Modelo de entrada
class Curriculo(BaseModel):
    texto: str

# 🔹 Funções utilitárias
def extrair_emails(texto: str) -> List[str]:
    pattern = r'[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+'
    return re.findall(pattern, texto)

def extrair_telefones(texto: str) -> List[str]:
    pattern = r'(?:\+55\s?)?(?:\(?\d{2}\)?[\s-]?)?\d{4,5}[\s-]?\d{4}'
    return re.findall(pattern, texto)

def extrair_nomes(texto: str) -> List[str]:
    doc = nlp(texto)
    nomes = [ent.text for ent in doc.ents if ent.label_ == "PER"]
    return nomes

# 🔹 Matcher híbrido
def extrair_hard_skills_hibrido(texto: str, similarity_threshold=0.88) -> List[str]:
    doc = nlp(texto)
    skills_encontradas = set()

    # Matcher exato
    matches = matcher(doc)
    for match_id, start, end in matches:
        skills_encontradas.add(doc[start:end].text)

    # Similaridade semântica
    for token in doc:
        for skill in all_skills:
            skill_doc = nlp(skill)
            sim = token.similarity(skill_doc)
            if sim >= similarity_threshold:
                skills_encontradas.add(skill)

    return list(skills_encontradas)

# 🔹 Classificação refinada
def classificar_perfil_refinado(hard_skills: List[str], threshold=0.3) -> Dict[str, any]:
    score = {categoria: 0 for categoria in CATEGORIAS}
    for categoria, skills in CATEGORIAS.items():
        for skill in skills:
            if skill in hard_skills:
                score[categoria] += 1

    confiancas = {}
    for categoria, count in score.items():
        confiancas[categoria] = round(count / len(CATEGORIAS[categoria]), 2) if CATEGORIAS[categoria] else 0.0

    categorias_ativas = [cat for cat, conf in confiancas.items() if conf >= threshold]

    # Regra: Frontend + Backend = Fullstack
    if "Frontend" in categorias_ativas and "Backend" in categorias_ativas:
        categorias_ativas.append("Fullstack")
        confiancas["Fullstack"] = round(
            (confiancas.get("Frontend",0) + confiancas.get("Backend",0)) / 2, 2
        )

    perfil_principal = max(confiancas, key=confiancas.get)
    return {"perfil": perfil_principal, "categorias": categorias_ativas, "confiancas": confiancas}

# 🔹 Endpoint principal
@router.post("/analisar")
async def analisar(curriculo: Curriculo):
    texto = curriculo.texto

    emails = extrair_emails(texto)
    telefones = extrair_telefones(texto)
    hard_skills = extrair_hard_skills_hibrido(texto)
    nomes_detectados = extrair_nomes(texto)
    perfil_info = classificar_perfil_refinado(hard_skills)

    return {
        "emails": emails or [],
        "telefones": telefones or [],
        "hard_skills": hard_skills or [],
        "nomes_detectados": nomes_detectados or [],
        "perfil": perfil_info.get("perfil", ""),
        "confianca": perfil_info["confiancas"].get(perfil_info["perfil"], 0.0)  # 🔹 Corrigido aqui
    }