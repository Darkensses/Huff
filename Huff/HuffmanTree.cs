using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;


class HuffmanTree
{
    public class Node
    {
        public NodeHuf element;
        public Node nodeNext;

        public Node(NodeHuf element, Node next = null)
        {
            this.element = element;
            nodeNext = next;
        }
    }

    //Cosas para crear una lista
    /// <summary>
    /// La clase tendra la característica de una lista enlazada,
    /// por lo tanto los atributos 'Head', 'Tail' y 'count' serán necesarios.
    /// </summary>
    Node Head { get; set; }
    Node Tail { get; set; }
    public int Count { get; private set; }
    NodeHuf Root { get; set; }

    public void Add(NodeHuf nodeHuf)
    {
        Count++;
        Node aux;
        if (Head == null || Head.element.Frequency > nodeHuf.Frequency)
        {
            Head = new Node(nodeHuf, Head);
        }
        else
        {
            aux = Head;
            while (aux.nodeNext != null && aux.nodeNext.element.Frequency <= nodeHuf.Frequency)
            {
                aux = aux.nodeNext;
            }
            aux.nodeNext = new Node(nodeHuf, aux.nodeNext);
            //aux = new Node(nodeHuf, aux.nodeNext);
        }

        if (Count == 1)
            Tail = Head;
        else
        {
            aux = Head;
            while (aux.nodeNext != null)
                aux = aux.nodeNext;
            Tail = aux;
        }

    }

    //algoritmo de huffman
    public void Build()
    {
        while (Count > 1)
        {
            NodeHuf newNH = new NodeHuf();
            newNH.Symbol = -666;
            newNH.Left = Head.element;
            Head = Head.nodeNext;
            newNH.Right = Head.element;
            Head = Head.nodeNext;
            newNH.Frequency = newNH.Left.Frequency + newNH.Right.Frequency;
            Count -= 2;
            Add(newNH);
        }
        //Indicamos la raíz del árbol de huffman.
        Root = Head.element;

        //La cabeza y cola de la lista enlazada ya no serán utilizadas.
        Head = Tail = null;

    }

    public BitArray Encode(short[] source)
    {
        List<bool> encodedSource = new List<bool>();

        for (int i = 0; i < source.Length; i++)
        {
            List<bool> encodedSymbol = this.Root.Traverse(source[i], new List<bool>());
            encodedSource.AddRange(encodedSymbol);
        }

        BitArray bits = new BitArray(encodedSource.ToArray());

        return bits;
    }

    public short[] Decode(BitArray bits)
    {
        NodeHuf current = this.Root;
        short[] decoded = new short[bits.Length];
        int i = 0;

        foreach (bool bit in bits)
        {
            if (bit)
            {
                if (current.Right != null)
                {
                    current = current.Right;
                }
            }
            else
            {
                if (current.Left != null)
                {
                    current = current.Left;
                }
            }

            //Con este método sabremos cuando se ha llegado a un símbolo u hoja.
            if (IsLeaf(current))
            {
                //decoded += current.Symbol;
                decoded[i] = current.Symbol;
                i++;
                current = this.Root;
            }
        }

        return decoded;
    }

    public bool IsLeaf(NodeHuf node)
    {
        return (node.Left == null && node.Right == null);
    }
}
    