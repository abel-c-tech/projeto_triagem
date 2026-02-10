def classificar_perfil(doc, nlp):
    perfis = {
        "Backend": [
            "linguagem de programação",
            "desenvolvimento backend",
            "api",
            "banco de dados",
            "servidor"
        ],
        "Frontend": [
            "desenvolvimento frontend",
            "interface do usuário",
            "web",
            "html",
            "css",
            "javascript"
        ],
        "Fullstack": [
            "desenvolvimento web",
            "frontend",
            "backend",
            "aplicações web"
        ],
        "Data": [
            "análise de dados",
            "estatística",
            "machine learning",
            "inteligência artificial"
        ],
        "DevOps": [
            "computação em nuvem",
            "containerização",
            "infraestrutura",
            "automação"
        ],
        "Design/Engenharia": [
            "engenharia",
            "design",
            "modelagem",
            "software de engenharia"
        ]
    }

    perfil_scores = {}

    for perfil, conceitos in perfis.items():
        conceitos_docs = [nlp(c) for c in conceitos]
        score = 0.0
        count = 0

        # 🔹 Expressões (chunks)
        for chunk in doc.noun_chunks:
            if not chunk.has_vector:
                continue

            for conceito_doc in conceitos_docs:
                sim = chunk.similarity(conceito_doc)
                if sim >= 0.65:
                    score += sim
                    count += 1

        # 🔹 Tokens isolados (python, java, docker, etc.)
        for token in doc:
            if token.is_stop or token.is_punct or not token.has_vector:
                continue

            for conceito_doc in conceitos_docs:
                sim = token.similarity(conceito_doc)
                if sim >= 0.60:
                    score += sim
                    count += 1

        perfil_scores[perfil] = score / count if count > 0 else 0.0

    melhor_score = max(perfil_scores.values())

    if melhor_score == 0:
        return "Indefinido", 0.0

    perfil_final = max(perfil_scores, key=perfil_scores.get)
    confianca = round(perfil_scores[perfil_final], 3)

    return perfil_final, confianca