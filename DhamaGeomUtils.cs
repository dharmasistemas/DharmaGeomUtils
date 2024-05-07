namespace DhamaGeomUtils
{
class DhaGeometry
{
          /// <summary>
        /// Método para ajustar a inclinação de linhas aos eixos X ou Y.
        /// Usado para tratar a tolerância de inclinação de linhas no Revit
        /// Ver: https://help.autodesk.com/view/RVT/2019/ENU/?guid=GUID-E8BF9268-4C26-439E-BB27-50EBF3638D3A
        /// Fonte: Adaptação de código gerado pelo ChatGPT em 23-02-2024
        /// </summary>
        /// <param name="objPto1">Coordenada do ponto inicial da linha.</param>
        /// <param name="objPto2">Coordenada do ponto final da linha.</param>
        /// <param name="dblTolMin">Tolerância mínima da inclinação. Valor padrão: 1e-9.</param>
        /// <param name="dblTolMax">Tolerância máxima da inclinação. Valor padrão: 0.2.</param>
        /// <returns>Retorna lista com as coordenadas ajustadas.</returns>
        internal static IList<XYZ> LineAdjustment(XYZ objPto1, XYZ objPto2, double dblTolMin = 1e-9, double dblTolMax = 0.2)
        {
            // Declaração de variáveis
            IList<XYZ> lstCoordAjustadas = new List<XYZ>();
            XYZ objPto1Ajust, objPto2Ajust;
            double dblX1, dblY1, dblZ1, dblX2, dblY2, dblZ2, dblSlope, dblTolComp;

            // Coordenadas das extremidades da linha (x1, y1) e (x2, y2)
            dblX1 = objPto1.X;
            dblY1 = objPto1.Y;
            dblZ1 = objPto1.Z;
            dblX2 = objPto2.X;
            dblY2 = objPto2.Y;
            dblZ2 = objPto2.Z;

            // Calcular a inclinação atual da linha
            dblSlope = (dblY2 - dblY1) / (dblX2 - dblX1);

            // Verificar se a inclinação está dentro do intervalo desejado
            dblTolComp = 1e-3;
            if (Math.Abs(dblSlope) > dblTolMin && Math.Abs(dblSlope) < Math.Tan(dblTolMax * Math.PI / 180.0) + dblTolComp)
            {
                // Ajustar a inclinação para alinhar com o eixo X ou Y
                if (Math.Abs(dblSlope) < Math.Tan(dblTolMax * Math.PI / 180.0) + dblTolComp)
                {
                    // Se a inclinação for próxima a 0, ajuste para alinhar com o eixo X
                    dblY2 = dblY1;
                }
                else
                {
                    // Caso contrário, ajuste para alinhar com o eixo Y
                    dblX2 = dblX1;
                }

                // Retorna coordenadas ajustadas da linha
                objPto1Ajust = new XYZ(dblX1, dblY1, dblZ1);
                objPto2Ajust = new XYZ(dblX2, dblY2, dblZ2);
                lstCoordAjustadas.Add(objPto1Ajust);
                lstCoordAjustadas.Add(objPto2Ajust);
            }
            else
            {
                // Se a linha já está ajustada, então retornar as mesmas coordenadas de entrada
                lstCoordAjustadas.Add(objPto1);
                lstCoordAjustadas.Add(objPto2);
            }

            return lstCoordAjustadas;
        }

        /// <summary>
        /// Obtem o ponto de intersecção de uma linha em uma face do terreno.
        /// </summary>
        /// <param name="lstFacesTerreno">Faces do terreno.</param>
        /// <param name="objLinha">Linha a checar intersecção com o terreno.</param>
        /// <returns>Retorna ponto de intersecção da linha em uma face do terreno.</returns>
        internal static XYZ GetIntersectionLineTerrain1(List<Face> lstFacesTerreno, Curve objLinha)
        {
            // Declaração de variáveis
            XYZ objPtoProj;
            Face objFace;
            SetComparisonResult objResProj;

            // Procurar ponto de intersecção
            objPtoProj = null;
            foreach (Face objFace in lstFacesTerreno)
            {
                objResProj = objFace.Intersect(objLinha, out IntersectionResultArray arrIntersRes);
                if (objResProj == SetComparisonResult.Overlap)
                {
                    objPtoProj = arrIntersRes.get_Item(0).XYZPoint;
                    break;
                }
            }

            return objPtoProj;
        }

        /// <summary>
        /// Obtém as faces de um elemento topography (superfície topográfica)
        /// </summary>
        /// <param name="objRefTerreno">Referência a geometria do elemento topography.</param>
        /// <returns>Retorna dados das faces do elemento topography.</returns>
        internal static List<Face> GetTopographyFaces(Element objTopoElem)
        {
            // Declaração de variáveis
            double dblExtrusao;

            GeometryElement objElemGeom;
            Mesh objMalha;
            MeshTriangle objTriangulo;
            XYZ objPto1, objPto2, objPto3, objNormalTriang;
            Curve objCurva1, objCurva2, objCurva3;
            CurveLoop objContorno;
            SolidOptions objOpcSolido;
            Solid objSolido;

            Face objFaceExtrusao;
            List<Face> lstFacesTerreno;
            List<Curve> lstContorno;
            List<CurveLoop> lstContornos;

            // Obter geometria do elemento
            objElemGeom = objTopoElem.get_Geometry(new Options());

            // Obter faces da superfície topográfica
            lstFacesTerreno = new List<Face>();
            foreach (GeometryObject objGeomObjeto in objElemGeom)
            {
                if (objGeomObjeto is Mesh)
                {
                    // Obter malha do terreno
                    objMalha = objGeomObjeto as Mesh;

                    // Obter faces da malha
                    for (int i = 0; i < objMalha.NumTriangles; i++)
                    {
                        // Obter vértices do triângulo
                        objTriangulo = objMalha.get_Triangle(i);
                        objPto1 = objTriangulo.get_Vertex(0);
                        objPto2 = objTriangulo.get_Vertex(1);
                        objPto3 = objTriangulo.get_Vertex(2);

                        // O try/catch é necessário para evitar erros gerados quando as faces dos triângulos
                        // são menores que o comprimento mínimo permitido pelo Revit. Neste caso, a face será ignorada
                        try
                        {
                            // Obter face do terreno
                            objCurva1 = Line.CreateBound(objPto1, objPto2);
                            objCurva2 = Line.CreateBound(objPto2, objPto3);
                            objCurva3 = Line.CreateBound(objPto3, objPto1);
                            lstContorno = new List<Curve>() { objCurva1, objCurva2, objCurva3 };
                            objContorno = CurveLoop.Create(lstContorno);
                            lstContornos = new List<CurveLoop>() { objContorno };
                            objNormalTriang = objContorno.GetPlane().Normal;
                            dblExtrusao = 0.0328084; // 1 cm
                            objOpcSolido = new SolidOptions(ElementId.InvalidElementId, ElementId.InvalidElementId);
                            objSolido = GeometryCreationUtilities.CreateExtrusionGeometry(lstContornos, objNormalTriang, dblExtrusao, objOpcSolido);
                            objFaceExtrusao = DhaGeometry.GetSolidFaceByNormal(objSolido, objNormalTriang);

                            // Armazenar face do terreno
                            lstFacesTerreno.Add(objFaceExtrusao);
                        }
                        catch (Exception)
                        {
                            throw;
                        }
                    }
                }
            }

            return lstFacesTerreno;
        }
}
}
  
}
