using UnityEngine;

public class FPS : MonoBehaviour
{
    protected float frameCount;
    protected float elapsedTime;

    [SerializeField]
    private float fps;

    [SerializeField]
    private float fpsAvg;

    [SerializeField]
    private float fpsMin;

    [SerializeField]
    private float fpsMax;

    private bool isInitialized;


    public float Fps { get { return this.fps; } }
    public float FpsAvg { get { return this.fpsAvg; } }
    public float FpsMin { get { return this.fpsMin; } }
    public float FpsMax { get { return this.fpsMax; } }
    private void Update()
    {
        // NOTE:
        // Maybe this is not strict.
        // this.fps = 1 / Time.deltaTime;

        this.frameCount += 1;
        this.elapsedTime += Time.deltaTime;

        if (this.elapsedTime > 1f)
        {
            this.fps = this.frameCount / this.elapsedTime;
            this.frameCount = 0;
            this.elapsedTime = 0;

            if (this.isInitialized)
            {
                this.fpsAvg = (this.fpsAvg + this.fps) / 2;
                this.fpsMin = Mathf.Min(this.fpsMin, this.fps);
                this.fpsMax = Mathf.Max(this.fpsMax, this.fps);
            }
            else
            {
                this.isInitialized = true;

                this.fpsAvg = this.fps;
                this.fpsMin = this.fps;
                this.fpsMax = this.fps;
            }
        }
    }
    // private void OnGUI()
    // {
    //     GUI.Label(new Rect(Screen.width / 2f, 10, 100, 50), $"FPS: {fps:0.00}");
    //     GUI.Label(new Rect(Screen.width / 2f, 25, 100, 50), $"AVG: {fpsAvg:0.00}");
    //     GUI.Label(new Rect(Screen.width / 2f, 40, 100, 50), $"Min: {fpsMin:0.00}");
    //     GUI.Label(new Rect(Screen.width / 2f, 55, 100, 50), $"Max: {fpsAvg:0.00}");
    // }
    public virtual void Reset()
    {
        this.frameCount = 0;
        this.elapsedTime = 0;

        this.isInitialized = false;

        this.fps = 0;
        this.fpsAvg = 0;
        this.fpsMin = 0;
        this.fpsMax = 0;
    }
}
