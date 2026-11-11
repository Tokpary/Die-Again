using System.Collections;
using UnityEngine;
using Code.Scripts.UI;

public class MainMenuCurtains : MonoBehaviour
{
    [SerializeField] private MainMenuOption botonJugar;
    [SerializeField] private Transform paredIzquierda;
    [SerializeField] private Transform paredDerecha;

    [SerializeField] private Transform camara;
    [SerializeField] private Transform breakpoint;
    [SerializeField] private float duracionMovimientoCamara = 2f;

    // Ajusta estos desplazamientos seg�n la orientaci�n de las paredes.
    [SerializeField]
    private Vector3 desplazamientoIzquierda =
        new Vector3(-5f, 0f, 0f);
    [SerializeField]
    private Vector3 desplazamientoDerecha =
        new Vector3(5f, 0f, 0f);

    [SerializeField] private float duracion = 1.5f;

    private Vector3 posicionInicialIzquierda;
    private Vector3 posicionInicialDerecha;
    private bool abiertas;
    private Coroutine movimientoCortinas;

    private void Awake()
    {
        posicionInicialIzquierda = paredIzquierda.localPosition;
        posicionInicialDerecha = paredDerecha.localPosition;
    }

    private void OnEnable()
    {
        botonJugar.OptionClicked += AlPulsarOpcion;
    }

    private void OnDisable()
    {
        botonJugar.OptionClicked -= AlPulsarOpcion;
    }

    private void AlPulsarOpcion(MainMenuOptionType opcion)
    {
        if (opcion == MainMenuOptionType.Play && !abiertas)
        {
            if (movimientoCortinas != null) StopCoroutine(movimientoCortinas);
            movimientoCortinas = StartCoroutine(Abrir());
        }
    }

    private IEnumerator Abrir()
    {
        abiertas = true;

        Vector3 destinoIzquierda =
            posicionInicialIzquierda + desplazamientoIzquierda;
        Vector3 destinoDerecha =
            posicionInicialDerecha + desplazamientoDerecha;

        float tiempo = 0f;

        while (tiempo < duracion)
        {
            tiempo += Time.deltaTime;
            float progreso = Mathf.Clamp01(tiempo / duracion);
            progreso = Mathf.SmoothStep(0f, 1f, progreso);

            paredIzquierda.localPosition = Vector3.Lerp(
                posicionInicialIzquierda, destinoIzquierda, progreso);

            paredDerecha.localPosition = Vector3.Lerp(
                posicionInicialDerecha, destinoDerecha, progreso);

            yield return null;
        }

        paredIzquierda.localPosition = destinoIzquierda;
        paredDerecha.localPosition = destinoDerecha;
        movimientoCortinas = null;

       // yield return StartCoroutine(MoverCamara());
    }

    public void Cerrar()
    {
        if (!abiertas) return;
        if (movimientoCortinas != null) StopCoroutine(movimientoCortinas);
        movimientoCortinas = StartCoroutine(CerrarCortinas());
    }

    private IEnumerator CerrarCortinas()
    {
        Vector3 inicioIzquierda = paredIzquierda.localPosition;
        Vector3 inicioDerecha = paredDerecha.localPosition;
        float tiempo = 0f;

        while (tiempo < duracion)
        {
            tiempo += Time.deltaTime;
            float progreso = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(tiempo / duracion));
            paredIzquierda.localPosition = Vector3.Lerp(inicioIzquierda, posicionInicialIzquierda, progreso);
            paredDerecha.localPosition = Vector3.Lerp(inicioDerecha, posicionInicialDerecha, progreso);
            yield return null;
        }

        paredIzquierda.localPosition = posicionInicialIzquierda;
        paredDerecha.localPosition = posicionInicialDerecha;
        abiertas = false;
        movimientoCortinas = null;
    }

 /*   private IEnumerator MoverCamara()
    {
        Vector3 posicionInicial = camara.position;
        Quaternion rotacionInicial = camara.rotation;

        float tiempo = 0f;

        while (tiempo < duracionMovimientoCamara)
        {
            tiempo += Time.deltaTime;
            float progreso = Mathf.Clamp01(tiempo / duracionMovimientoCamara);
            progreso = Mathf.SmoothStep(0f, 1f, progreso);

            camara.position = Vector3.Lerp(
                posicionInicial, breakpoint.position, progreso);

            camara.rotation = Quaternion.Slerp(
                rotacionInicial, breakpoint.rotation, progreso);

            yield return null;
        }

        camara.position = breakpoint.position;
        camara.rotation = breakpoint.rotation;
    }*/
}