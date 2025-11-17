FROM python:3.11-slim

WORKDIR /app

# Install system dependencies (including ffmpeg for pydub)
RUN apt-get update && apt-get install -y \
    build-essential \
    ffmpeg \
    && rm -rf /var/lib/apt/lists/*

# Copy requirements first for better caching
COPY requirements.txt .
RUN pip install --no-cache-dir --default-timeout=1000 --extra-index-url https://download.pytorch.org/whl/cpu -r requirements.txt

# Copy application code
COPY . .

# Create models directory
RUN mkdir -p /app/models

EXPOSE 8765

CMD ["python", "main.py"]





























