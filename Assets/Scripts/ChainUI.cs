using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class ChainUI : MonoBehaviour
{
    public JointChain chain;
    public Slider jointSlider;
    public Text jointLabel;
    public Text angleText;
    public Text hintText;

    private int lastCount;

    void Start()
    {
        if (chain == null) chain = FindObjectOfType<JointChain>();
        if (chain == null)
        {
            enabled = false;
            return;
        }

        lastCount = Mathf.Clamp(chain.jointCount, 2, 12);

        if (jointSlider != null)
        {
            jointSlider.minValue = 2;
            jointSlider.maxValue = 12;
            jointSlider.wholeNumbers = true;
            jointSlider.SetValueWithoutNotify(lastCount);
            jointSlider.onValueChanged.AddListener(OnSliderChanged);
        }

        if (jointLabel != null)
            jointLabel.text = "Joints: " + lastCount;

        if (hintText != null)
            hintText.text = "Drag the red tip. Root is fixed.";
    }

    void OnSliderChanged(float v)
    {
        int n = Mathf.RoundToInt(v);
        if (n == lastCount) return;

        lastCount = n;
        chain.Rebuild(n);

        if (jointLabel != null)
            jointLabel.text = "Joints: " + n;
    }

    void Update()
    {
        if (chain == null || angleText == null) return;

        float[] a = chain.GetAngles();
        if (a == null) return;

        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < a.Length; i++)
            sb.AppendLine("Joint " + (i + 1) + ": " + a[i].ToString("F1") + "бу");

        angleText.text = sb.ToString();
    }
}