using System.Collections.Generic;

namespace LLM
{
    namespace RAG
    {
        public class TextSplitter
        {
            int maxChunkSize;
            int chunkOverlap;
            string[] separators;

            public TextSplitter(int maxChunkSize = 200, int chunkOverlap = 9, string[] separators = null)
            {
                this.maxChunkSize = maxChunkSize;
                this.chunkOverlap = chunkOverlap;
                this.separators = separators ?? new string[] {
                    "\n\n",
                    "\n",
                    " ",
                };
            }


            public List<string> SplitText(string text)
            {
                // Eliminar \r
                text = text.Replace("\r", "");

                List<string> chunks = new List<string>();
                int currChar = 0;

                // Recorre todos los caracteres del texto
                while (currChar < text.Length)
                {
                    string bestSeparator = "";
                    int bestSeparatorPos = -1;

                    // Ultimo caracter en el que buscar un separador. Si es mayor que el ultimo caracter del texto, el
                    // sera el ultimo del texto. Si no, sera el ultimo caracter del chunk
                    int lastChar = currChar + maxChunkSize > text.Length ? text.Length : currChar + maxChunkSize;

                    // Tamano de la busqueda. Como se busca el ultimo, se hara desde el final hasta el principio, por lo
                    // que si el tamano del chunk es mayor al texto que queda por trocear, el tamano sera el del texto
                    // que quede por trocear, y si no es mayor, el tamano sera el del tamano del chunk
                    int inverseSearchSize = maxChunkSize > text.Length - currChar ? text.Length - currChar : maxChunkSize;

                    // Si el ultimo caracter en el que buscar es el final del texto, el mejor separador esta en el final del texto
                    if (lastChar >= text.Length)
                    {
                        bestSeparatorPos = lastChar;
                    }
                    // Si no, recorre todos los separadores
                    else
                    {
                        // Busca el separador con la mejor posicion (el mas proximo a la ultima palabra que quepa en el chunk).
                        // Si es un salto de parrafo, mete todo el texto directamente al chunk
                        for (int i = 0; i < separators.Length && bestSeparator != "\n\n"; i++)
                        {
                            // Encuentra la ultima aparicion (entre el caracter actual y el ultimo del chunk) del caracter separador 
                            int pos = text.LastIndexOf(separators[i], lastChar, inverseSearchSize);

                            // Se ha encontrado un separador mejor si su posicion no es -1, la posicion es mayor que la
                            // mejor anterior, y el texto entre el caracter actual y el separador (incluyendo todos los
                            // caracteres que ocupa el separador) caben dentro del chunk
                            if (pos != -1 &&
                                (bestSeparatorPos == -1 || pos > bestSeparatorPos) &&
                                (pos + separators[i].Length - currChar) <= maxChunkSize)
                            {
                                bestSeparatorPos = pos;
                                bestSeparator = separators[i];
                            }
                        }
                    }

                    // Calcula el tamano del chunk. Si no ha encontrado un separador mejor, el tamano del chunk es
                    // la longitud de todo el texto a partir del caracter actual. Si encuentra un separador mejor,
                    // es la longitud del texto entre el caracter actual y el separador (incluyendo todos los
                    // caracteres que ocupa el separador)
                    int currentChunkSize = bestSeparatorPos == -1 ? inverseSearchSize : bestSeparatorPos + bestSeparator.Length - currChar;

                    // Extrae el chunk de texto y lo anade a los chunks
                    string chunk = text.Substring(currChar, currentChunkSize - bestSeparator.Length);
                    chunks.Add(chunk);

                    // Actualiza el caracter actual
                    currChar += currentChunkSize;

                    // Si hay overlap, nuevo caracter actual no supera el final del texto, y no hay un salto de parrafo en el chunk
                    if (chunkOverlap > 0 && currChar < text.Length && bestSeparator != "\n\n")
                    {
                        // Primer caracter desde el que buscar un separador. Si hay menos caracteres para hacer overlap
                        // que el tamano maximo, se empieza desde el principio, y si no, desde el caracter a chunkOverlap 
                        // caracteres del ultimo del chunk actual
                        int firstChar = currChar - chunkOverlap < 0 ? 0 : currChar - chunkOverlap;

                        // Tamano de la busqueda. Como se busca el primero, se hara desde el principio hasta el final, por lo
                        // que si el tamano del overlap es mayor al texto al que se le puede hacer overlap, el tamano sera el
                        // del texto al que se puede hacer overlap, y si no es mayor, el tamano sera el del overlap establecido
                        int searchSize = chunkOverlap > currChar - chunkOverlap ? currChar - chunkOverlap : chunkOverlap;

                        foreach (string separator in separators)
                        {
                            // Encuentra la primera aparicion (entre el primer caracter del overlap y el ultimo del chunk) del caracter separador 
                            int pos = text.IndexOf(separator, firstChar, searchSize);

                            // Se ha encontrado un separador mejor si su posicion no es -1, la posicion es menor que la
                            // mejor anterior, y el texto entre el caracter actual y el separador (incluyendo todos los
                            // caracteres que ocupa el separador) caben dentro del chunk
                            if (pos != -1 &&
                                (bestSeparatorPos == -1 || pos < bestSeparatorPos) &&
                                (pos + separator.Length - currChar) <= maxChunkSize)
                            {
                                bestSeparatorPos = pos;
                                bestSeparator = separator;
                            }
                        }

                        // Calcula el tamano del overlap. Si no ha encontrado un separador mejor, es el tamano completo.
                        // Si encuentra un separador mejor, es la longitud del texto entre el ultimo caracter del chunk
                        // actual y el separador (incluyendo todos los caracteres que ocupa el separador)
                        int overlapPortionSize = bestSeparatorPos == -1 ?
                            searchSize :
                            currChar - (bestSeparatorPos + bestSeparator.Length);

                        // Actualiza el nuevo caracter actual para que el siguiente chunk empiece desde el overlap
                        currChar -= overlapPortionSize;
                    }
                }

                return chunks;
            }

        }
    }
}